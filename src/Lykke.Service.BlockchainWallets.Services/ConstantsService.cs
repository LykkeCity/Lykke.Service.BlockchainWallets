using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.Services;

namespace Lykke.Service.BlockchainWallets.Services
{
    [UsedImplicitly]
    public class ConstantsService : IConstantsService
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly ICapabilitiesService _capabilitiesService;
        private readonly ConcurrentDictionary<string, AddressExtensionConstantsDto> _cache;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;
        
        public ConstantsService(
            IBlockchainIntegrationService blockchainIntegrationService,
            ICapabilitiesService capabilitiesServoce)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _capabilitiesService = capabilitiesServoce;

            _cache = new ConcurrentDictionary<string, AddressExtensionConstantsDto>();
            _locks = new ConcurrentDictionary<string, SemaphoreSlim>();
        }


        public async Task<AddressExtensionConstantsDto> GetAddressExtensionConstantsAsync(string blockchainType)
        {
            if (string.IsNullOrEmpty(blockchainType))
            {
                throw new ArgumentException("Should not be null or empty", nameof(blockchainType));
            }

            var apiClient = _blockchainIntegrationService.TryGetApiClient(blockchainType);
            if (apiClient == null)
            {
                throw new NotSupportedException($"Blockchain type [{blockchainType}] is not supported.");
            }

            AddressExtensionConstantsDto result;

            var @lock = _locks.GetOrAdd(blockchainType, x => new SemaphoreSlim(1));

            await @lock.WaitAsync();

            try
            {
                if (!_cache.TryGetValue(blockchainType, out result))
                {
                    var isAddressExtensionRequired = await _capabilitiesService.IsPublicAddressExtensionRequiredAsync(blockchainType);
                    
                    var constants = isAddressExtensionRequired 
                        ? await apiClient.GetConstantsAsync()
                        : null;

                    if (constants?.PublicAddressExtension != null)
                    {
                        result = new AddressExtensionConstantsDto
                        {
                            AddressExtensionDisplayName = constants.PublicAddressExtension.DisplayName,
                            BaseAddressDisplayName = constants.PublicAddressExtension.BaseDisplayName,
                            Separator = constants.PublicAddressExtension.Separator,
                            TypeForDeposit = AddressExtensionTypeForDeposit.Required,
                            TypeForWithdrawal = AddressExtensionTypeForWithdrawal.Optional
                        };
                    }
                    else
                    {
                        result = new AddressExtensionConstantsDto
                        {
                            TypeForDeposit = AddressExtensionTypeForDeposit.NotSupported,
                            TypeForWithdrawal = AddressExtensionTypeForWithdrawal.NotSupported
                        };
                    }

                    result.SeparatorExists = result.Separator != default(char);
                    _cache.TryAdd(blockchainType, result);
                }
            }
            finally
            {
                @lock.Release();
            }

            return result;
        }
    }
}
