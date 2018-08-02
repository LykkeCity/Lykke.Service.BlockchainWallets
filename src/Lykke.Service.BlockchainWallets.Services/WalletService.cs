using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.BlockchainSignFacade.Client;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Contract.Events;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration;


namespace Lykke.Service.BlockchainWallets.Services
{
    [UsedImplicitly]
    public class WalletService : IWalletService
    {
        private readonly ICqrsEngine _cqrsEngine;
        private readonly IWalletRepository _walletRepository;
        private readonly IAdditionalWalletRepository _additionalWalletRepository;
        private readonly IBlockchainSignFacadeClient _blockchainSignFacadeClient;
        private readonly IAddressParser _addressParser;
        private readonly IFirstGenerationBlockchainWalletRepository _firstGenerationBlockchainWalletRepository;
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;
        private readonly ICapabilitiesService _capabilitiesService;
        private readonly IAddressService _addressService;
        private readonly ILegacyWalletService _legacyWalletService;
        private readonly ILog _log;

        public WalletService(
            ICqrsEngine cqrsEngine,
            IWalletRepository walletRepository,
            IAdditionalWalletRepository additionalWalletRepository,
            IBlockchainSignFacadeClient blockchainSignFacadeClient,
            IAddressParser addressParser,
            IFirstGenerationBlockchainWalletRepository firstGenerationBlockchainWalletRepository,
            IAssetsServiceWithCache assetsServiceWithCache,
            ICapabilitiesService capabilitiesService,
            IAddressService addressService,
            ILegacyWalletService legacyWalletService,
            ILog log)
        {
            _cqrsEngine = cqrsEngine;
            _walletRepository = walletRepository;
            _additionalWalletRepository = additionalWalletRepository;
            _blockchainSignFacadeClient = blockchainSignFacadeClient;
            _addressParser = addressParser;
            _firstGenerationBlockchainWalletRepository = firstGenerationBlockchainWalletRepository;
            _assetsServiceWithCache = assetsServiceWithCache;
            _capabilitiesService = capabilitiesService;
            _addressService = addressService;
            _legacyWalletService = legacyWalletService;
            _log = log;
        }

        private async Task<bool> AdditionalWalletExistsAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            return await _additionalWalletRepository.ExistsAsync(integrationLayerId, assetId, clientId);
        }

        public async Task<bool> DoesAssetExistAsync(string assetId)
        {
            var asset = await _assetsServiceWithCache.TryGetAssetAsync(assetId);

            return asset != null;
        }

        public async Task<WalletWithAddressExtensionDto> CreateWalletAsync(string blockchainType, string assetId, Guid clientId)
        {
            string address = null;

            if (blockchainType != SpecialBlockchainTypes.FirstGenerationBlockchain)
            {
                var wallet = await _blockchainSignFacadeClient.CreateWalletAsync(blockchainType);
                address = wallet.PublicAddress;

                var isAddressMappingRequired = await _capabilitiesService.IsAddressMappingRequiredAsync(blockchainType);
                var underlyingAddress = await _addressService.GetUnderlyingAddressAsync(blockchainType, address);

                if (isAddressMappingRequired && underlyingAddress == null)
                {
                    throw new ArgumentException($"Failed to get UnderlyingAddress for blockchainType={blockchainType} and address={address}");
                }

                await _walletRepository.AddAsync(blockchainType, assetId, clientId, address);
                var @event = new WalletCreatedEvent
                {
                    Address = address,
                    AssetId = assetId,
                    BlockchainType = blockchainType,
                    IntegrationLayerId = blockchainType
                };

                await _firstGenerationBlockchainWalletRepository.InsertOrReplaceAsync(new BcnCredentialsRecord
                {
                    Address = string.Empty,
                    AssetAddress = address,
                    ClientId = clientId.ToString(),
                    EncodedKey = string.Empty,
                    PublicKey = string.Empty,
                    AssetId = $"{blockchainType} ({assetId})"
                });

                _cqrsEngine.PublishEvent
                (
                    @event,
                    BlockchainWalletsBoundedContext.Name
                );

                return await ConvertWalletToWalletWithAddressExtensionAsync
                (
                    new WalletDto
                    {
                        Address = isAddressMappingRequired ? underlyingAddress : address,
                        AssetId = assetId,
                        BlockchainType = blockchainType,
                        ClientId = clientId
                    }
                );
            }

            address = await _legacyWalletService.CreateWalletAsync(clientId, assetId);

            return new WalletWithAddressExtensionDto()
            {
                Address = address,
                AssetId = assetId,
                BlockchainType = blockchainType,
                ClientId = clientId,
                BaseAddress = address
            };
        }

        public async Task<bool> DefaultWalletExistsAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            return await _walletRepository.ExistsAsync(integrationLayerId, assetId, clientId);
        }

        public async Task DeleteWalletsAsync(string blockchainType, string assetId, Guid clientId)
        {
            await DeleteAdditionalWalletsAsync(blockchainType, assetId, clientId);

            await DeleteDefaultWalletAsync(blockchainType, assetId, clientId);
        }

        private async Task DeleteAdditionalWalletsAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            await _additionalWalletRepository.DeleteAllAsync(integrationLayerId, assetId, clientId);
        }

        private async Task DeleteDefaultWalletAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            var wallet = await _walletRepository.TryGetAsync(integrationLayerId, assetId, clientId);

            await _walletRepository.DeleteIfExistsAsync(integrationLayerId, assetId, clientId);

            var address = wallet.Address;
            var @event = new WalletDeletedEvent
            {
                Address = address,
                AssetId = assetId,
                BlockchainType = integrationLayerId,
                IntegrationLayerId = integrationLayerId
            };

            _cqrsEngine.PublishEvent
            (
                @event,
                BlockchainWalletsBoundedContext.Name
            );
        }

        public async Task<WalletWithAddressExtensionDto> TryGetDefaultAddressAsync(string blockchainType, string assetId, Guid clientId)
        {
            var wallet = await _walletRepository.TryGetAsync(blockchainType, assetId, clientId);

            if (wallet != null)
            {
                if (await _capabilitiesService.IsAddressMappingRequiredAsync(blockchainType))
                {
                    var underlyingAddress = await _addressService.GetUnderlyingAddressAsync(blockchainType, wallet.Address);

                    wallet.Address = underlyingAddress ?? throw new ArgumentException($"Failed to get UnderlyingAddress for " +
                        $"blockchainType={blockchainType} and address={wallet.Address}");
                }

                return await ConvertWalletToWalletWithAddressExtensionAsync(wallet);
            }

            return null;
        }

        public async Task<WalletWithAddressExtensionDto> TryGetFirstGenerationBlockchainAddressAsync(string assetId, Guid clientId)
        {
            var asset = await _assetsServiceWithCache.TryGetAssetAsync(assetId);

            if (asset == null)
                return null;

            bool isErc20 = asset.Type == AssetType.Erc20Token;
            bool isEtherium = asset.Blockchain == Blockchain.Ethereum;
            bool isColoredCoin = assetId != SpecialAssetIds.SolarAssetId &&
                                 !string.IsNullOrEmpty(asset.BlockChainAssetId) &&
                                 asset.Blockchain == Blockchain.Bitcoin;

            var wallet = await _firstGenerationBlockchainWalletRepository.TryGetAsync(assetId, 
                clientId,
                isErc20, 
                isEtherium,
                isColoredCoin);

            if (wallet != null)
            {
                return new WalletWithAddressExtensionDto
                {
                    Address = wallet.Address,
                    AddressExtension = string.Empty,
                    AssetId = wallet.AssetId,
                    BaseAddress = wallet.Address,
                    BlockchainType = SpecialBlockchainTypes.FirstGenerationBlockchain,
                    ClientId = wallet.ClientId
                };
            }

            return null;
        }

        public async Task<Guid?> TryGetClientIdAsync(string blockchainType, string assetId, string address)
        {
            address = (await _addressService.GetVirtualAddressAsync(blockchainType, address)) ?? address;

            return (await _walletRepository.TryGetAsync(blockchainType, assetId, address))?.ClientId
                ?? (await _additionalWalletRepository.TryGetAsync(blockchainType, assetId, address))?.ClientId;
        }

        public async Task<bool> WalletExistsAsync(string blockchainType, string assetId, Guid clientId)
        {
            return await DefaultWalletExistsAsync(blockchainType, assetId, clientId)
                || await AdditionalWalletExistsAsync(blockchainType, assetId, clientId);
        }

        public async Task<(IEnumerable<WalletWithAddressExtensionDto>, string continuationToken)> GetClientWalletsAsync(Guid clientId, int take, string continuationToken)
        {
            var finalWallets = new List<WalletWithAddressExtensionDto>();
            var (wallets, token) = await _walletRepository.GetAllAsync(clientId, take, continuationToken);
            
            foreach (var wallet in wallets)
            {
                if (await _capabilitiesService.IsAddressMappingRequiredAsync(wallet.BlockchainType))
                {
                    var underlyingAddress = await _addressService.GetUnderlyingAddressAsync(wallet.BlockchainType, wallet.Address);
                    if (underlyingAddress == null)
                    {
                        _log.WriteError(nameof(GetClientWalletsAsync),
                            new { wallet.BlockchainType, wallet.Address },
                            new Exception("Failed to get underlyingAddress address"));
                    }
                    else
                    {
                        wallet.Address = underlyingAddress;

                        finalWallets.Add(await ConvertWalletToWalletWithAddressExtensionAsync(wallet));
                    }
                }
                else
                {
                    finalWallets.Add(await ConvertWalletToWalletWithAddressExtensionAsync(wallet));
                }
            }

            return (finalWallets, token);
        }

        private async Task<WalletWithAddressExtensionDto> ConvertWalletToWalletWithAddressExtensionAsync(WalletDto walletDto)
        {
            var parseAddressResult = await _addressParser.ExtractAddressParts(walletDto.BlockchainType, walletDto.Address);

            return new WalletWithAddressExtensionDto
            {
                Address = walletDto.Address,
                AddressExtension = parseAddressResult.AddressExtension,
                AssetId = walletDto.AssetId,
                BaseAddress = parseAddressResult.BaseAddress,
                BlockchainType = walletDto.BlockchainType,
                ClientId = walletDto.ClientId
            };
        }
    }
}
