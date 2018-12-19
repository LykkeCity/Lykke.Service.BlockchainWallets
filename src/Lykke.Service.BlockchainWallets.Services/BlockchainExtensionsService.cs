using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.Services;
using MoreLinq;

namespace Lykke.Service.BlockchainWallets.Services
{
    [UsedImplicitly]
    public class BlockchainExtensionsService : IBlockchainExtensionsService
    {
        #region Fields

        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly ConcurrentDictionary<string, bool> _cacheCapabilities;
        private readonly ConcurrentDictionary<string, AddressExtensionConstantsDto> _cacheConstants;
        private readonly Dictionary<string, bool> _cacheBlockchainAssetsReady;
        private readonly ConcurrentDictionary<(string blockchainType, string assetId), BlockchainAssetDto> _cacheBlockchainAssets;
        private readonly ILog _log;

        // TODO: Add new capabilities here.
        private readonly List<string> _supportedCapabilities = new List<string> { "IsPublicAddressExtensionRequired", "IsAddressMappingRequired" };

        private const int _apiCallRetryDelay = 30; // In seconds
        private readonly Dictionary<string, int> _blockchainConnectAttemptsDelays;
        #endregion

        #region Initialization
        public BlockchainExtensionsService(
            IBlockchainIntegrationService blockchainIntegrationService,
            ILogFactory logFactory)
        {
            if (logFactory == null)
                throw new ArgumentNullException(nameof(logFactory));
            _log = logFactory.CreateLog(this);

            _blockchainIntegrationService = blockchainIntegrationService ?? throw new ArgumentNullException(nameof(blockchainIntegrationService));

            _cacheCapabilities = new ConcurrentDictionary<string, bool>();
            _cacheConstants = new ConcurrentDictionary<string, AddressExtensionConstantsDto>();
            _cacheBlockchainAssets =new ConcurrentDictionary<(string blockchainType, string assetId), BlockchainAssetDto>();

            _blockchainConnectAttemptsDelays = new Dictionary<string, int>();
            
        }

        public void FireInitializationAndForget()
        {
            var apiCLients = _blockchainIntegrationService.GetApiClientsEnumerable()?.ToArray();
            var initializationTasks = new List<Task>(apiCLients.Count());

            apiCLients.ForEach(x =>
            {
                var task = new Task(async () =>
                {
                    _cacheBlockchainAssetsReady[x.Key] = false;
                    await InitializeOneAsync(x.Key, x.Value);
                });

                initializationTasks.Add(task);
            });

            initializationTasks.ForEach(x => x.Start());
        }

        private async Task InitializeOneAsync(string blockchainType, BlockchainApiClient apiClient)
        {
            while (true)
            {
                try
                {
                    // Capabilities
                    var capabilities = await apiClient.GetCapabilitiesAsync();

                    var key = $"{blockchainType}-IsPublicAddressExtensionRequired";
                    _cacheCapabilities.TryAdd(key, capabilities.IsPublicAddressExtensionRequired);

                    key = $"{blockchainType}-IsAddressMappingRequired";
                    _cacheCapabilities.TryAdd(key, capabilities.IsAddressMappingRequired);

                    // Constants
                    if (capabilities.IsPublicAddressExtensionRequired)
                    {
                        var constants = await apiClient.GetConstantsAsync();

                        var constantsDto = new AddressExtensionConstantsDto
                        {
                            TypeForDeposit = AddressExtensionTypeForDeposit.NotSupported,
                            TypeForWithdrawal = AddressExtensionTypeForWithdrawal.NotSupported
                        };

                        if (constants.PublicAddressExtension != null)
                        {
                            constantsDto = new AddressExtensionConstantsDto
                            {
                                AddressExtensionDisplayName = constants.PublicAddressExtension.DisplayName,
                                BaseAddressDisplayName = constants.PublicAddressExtension.BaseDisplayName,
                                Separator = constants.PublicAddressExtension.Separator,
                                TypeForDeposit = AddressExtensionTypeForDeposit.Required,
                                TypeForWithdrawal = AddressExtensionTypeForWithdrawal.Optional
                            };
                        }

                        constantsDto.SeparatorExists = constantsDto.Separator != default(char);
                        _cacheConstants.TryAdd($"{blockchainType}-Constants", constantsDto);
                    }

                    await apiClient.EnumerateAllAssetsAsync(50, (asset) =>
                    {
                        if (asset == null)
                            return;

                        var blockchainAssetDto = new BlockchainAssetDto()
                        {
                            Address = asset.Address,
                            AssetId = asset.AssetId,
                            Accuracy = asset.Accuracy,
                            Name = asset.Name
                        };

                        _cacheBlockchainAssets.TryAdd((blockchainType, asset.AssetId), blockchainAssetDto);
                    });

                    // Exit on success
                    if (_blockchainConnectAttemptsDelays.ContainsKey(blockchainType))
                        _blockchainConnectAttemptsDelays.Remove(blockchainType);

                    _cacheBlockchainAssetsReady[blockchainType] = true;

                    break;
                }
                catch (Exception ex)
                {
                    _log.Warning($"Unable to obtain and/or store in cache the capabilities or constants data for the blockchain type {blockchainType}. Will retry till success.", ex);

                    if (!_blockchainConnectAttemptsDelays.TryGetValue(blockchainType, out var delay))
                    {
                        delay = _apiCallRetryDelay;
                        _blockchainConnectAttemptsDelays.Add(blockchainType, delay);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(delay));

                    _blockchainConnectAttemptsDelays[blockchainType] = Math.Min(delay * 2, 600);
                }
            }
        }
        #endregion

        #region Private
        private bool? TryGetCapability(string blockchainType, string capability)
        {
            if (!_blockchainIntegrationService.BlockchainIsSupported(blockchainType))
            {
                _log.Warning($"Capability {capability} for unsupported blockchain type {blockchainType} was queried. Nothing to return.");
                return null;
            }

            if (!_supportedCapabilities.Contains(capability))
            {
                _log.Warning($"Unsupported capability {capability} for blockchain type {blockchainType} was queried. Nothing to return.");
            }

            var key = $"{blockchainType}-{capability}";

            if (_cacheCapabilities.TryGetValue(key, out var value))
                return value;

            _log.Warning($"Unable to provide a supported capability {capability} for the supported blockchain type {blockchainType}: cache does not contain the data. Checkup initialization state and settings.");
            return null;
        }
        #endregion

        #region Public

        public bool? IsPublicAddressExtensionRequired(string blockchainType)
        {
            return TryGetCapability(blockchainType, "IsPublicAddressExtensionRequired");
        }

        public bool? IsAddressMappingRequired(string blockchainType)
        {
            return TryGetCapability(blockchainType, "IsAddressMappingRequired");
        }

        public AddressExtensionConstantsDto TryGetAddressExtensionConstants(string blockchainType)
        {
            if (!_blockchainIntegrationService.BlockchainIsSupported(blockchainType))
            {
                _log.Warning($"Constants for unsupported blockchain type {blockchainType} were queried. Nothing to return.");
                return null;
            }

            return _cacheConstants.TryGetValue($"{blockchainType}-Constants", out var value)
                ? value
                : null;
        }

        public BlockchainAssetDto TryGetBlockchainAsset(string blockchainType, string assetId)
        {
            if (!_blockchainIntegrationService.BlockchainIsSupported(blockchainType))
            {
                _log.Warning($"Constants for unsupported blockchain type {blockchainType} were queried. Nothing to return.");
                return null;
            }

            if (!_cacheBlockchainAssetsReady[blockchainType])
                throw new
            return _cacheBlockchainAssets.TryGetValue((blockchainType, assetId), out var value)
                ? value
                : null;
        }

        #endregion
    }
}
