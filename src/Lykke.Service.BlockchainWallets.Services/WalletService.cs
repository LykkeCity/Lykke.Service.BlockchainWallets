using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Service.BlockchainSignFacade.Client;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.Commands;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Core.Services;


namespace Lykke.Service.BlockchainWallets.Services
{
    [UsedImplicitly]
    public class WalletService : IWalletService
    {
        private readonly ICapabilitiesService _capabilitiesService;
        private readonly IConstantsService _constantsService;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly IWalletRepository _walletRepository;
        private readonly IAdditionalWalletRepository _additionalWalletRepository;
        private readonly IBlockchainSignFacadeClient _blockchainSignFacadeClient;
        private readonly IAddressParser _addressParser;


        public WalletService(
            ICapabilitiesService capabilitiesService,
            IConstantsService constantsService,
            ICqrsEngine cqrsEngine,
            IWalletRepository walletRepository,
            IAdditionalWalletRepository additionalWalletRepository,
            IBlockchainSignFacadeClient blockchainSignFacadeClient,
            IAddressParser addressParser)
        {
            _capabilitiesService = capabilitiesService;
            _constantsService = constantsService;
            _cqrsEngine = cqrsEngine;
            _walletRepository = walletRepository;
            _additionalWalletRepository = additionalWalletRepository;
            _blockchainSignFacadeClient = blockchainSignFacadeClient;
            _addressParser = addressParser;
        }

        private async Task<bool> AdditionalWalletExistsAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            return await _additionalWalletRepository.ExistsAsync(integrationLayerId, assetId, clientId);
        }


        public async Task<WalletWithAddressExtensionDto> CreateWalletAsync(string blockchainType, string assetId, Guid clientId)
        {
            var wallet = await _blockchainSignFacadeClient.CreateWalletAsync(blockchainType);
            var address = wallet.PublicAddress;
            var command = new BeginBalanceMonitoringCommand
            {
                Address = address,
                AssetId = assetId,
                IntegrationLayerId = blockchainType
            };

            await _walletRepository.AddAsync(blockchainType, assetId, clientId, address);

            _cqrsEngine.SendCommand
            (
                command,
                BlockchainWalletsBoundedContext.Name,
                BlockchainWalletsBoundedContext.Name
            );

            return await ConvertWalletToWalletWithAddressExtensionAsync
            (
                new WalletDto
                {
                    Address = address,
                    AssetId = assetId,
                    BlockchainType = blockchainType,
                    ClientId = clientId
                }
            );
        }

        public async Task<bool> DefaultWalletExistsAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            return await _walletRepository.ExistsAsync(integrationLayerId, assetId, clientId);
        }

        public async Task DeleteWalletsAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            await DeleteAdditionalWalletsAsync(integrationLayerId, assetId, clientId);

            await DeleteDefaultWalletAsync(integrationLayerId, assetId, clientId);
        }

        private async Task DeleteAdditionalWalletsAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            await _additionalWalletRepository.DeleteAllAsync(integrationLayerId, assetId, clientId);
        }

        private async Task DeleteDefaultWalletAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            var wallet = await _walletRepository.TryGetAsync(integrationLayerId, assetId, clientId);
            var address = wallet.Address;
            var command = new EndBalanceMonitoringCommand
            {
                Address = address,
                AssetId = assetId,
                IntegrationLayerId = integrationLayerId
            };

            await _walletRepository.DeleteIfExistsAsync(integrationLayerId, assetId, clientId);

            _cqrsEngine.SendCommand
            (
                command,
                BlockchainWalletsBoundedContext.Name,
                BlockchainWalletsBoundedContext.Name
            );
        }

        public async Task<WalletWithAddressExtensionDto> TryGetDefaultAddressAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            var wallet = await _walletRepository.TryGetAsync(integrationLayerId, assetId, clientId);

            if (wallet != null)
            {
                return await ConvertWalletToWalletWithAddressExtensionAsync(wallet);
            }

            return null;
        }

        public async Task<Guid?> TryGetClientIdAsync(string integrationLayerId, string assetId, string address)
        {
            return (await _walletRepository.TryGetAsync(integrationLayerId, assetId, address))?.ClientId
                ?? (await _additionalWalletRepository.TryGetAsync(integrationLayerId, assetId, address))?.ClientId;
        }

        public async Task<bool> WalletExistsAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            return await DefaultWalletExistsAsync(integrationLayerId, assetId, clientId)
                || await AdditionalWalletExistsAsync(integrationLayerId, assetId, clientId);
        }

        public async Task<(IEnumerable<WalletWithAddressExtensionDto>, string continuationToken)> GetClientWalletsAsync(Guid clientId, int take, string continuationToken)
        {
            var (wallets, token) = await _walletRepository.GetAllAsync(clientId, take, continuationToken);
            
            return 
            (
                await Task.WhenAll(wallets.Select(ConvertWalletToWalletWithAddressExtensionAsync)),
                token
            );
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
