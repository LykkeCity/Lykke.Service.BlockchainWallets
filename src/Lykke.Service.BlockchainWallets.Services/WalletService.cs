using System;
using System.Threading.Tasks;
using Lykke.Cqrs;
using Lykke.Service.BlockchainSignFacade.Client;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.Domain.Wallet;
using Lykke.Service.BlockchainWallets.Core.Domain.Wallet.Commands;
using Lykke.Service.BlockchainWallets.Core.Services;


namespace Lykke.Service.BlockchainWallets.Services
{
    public class WalletService : IWalletService
    {
        private readonly ICqrsEngine _cqrsEngine;
        private readonly IWalletRepository _walletRepository;
        private readonly IAdditionalWalletRepository _additionalWalletRepository;
        private readonly IBlockchainSignFacadeClient _blockchainSignFacadeClient;


        public WalletService(
            ICqrsEngine cqrsEngine,
            IWalletRepository walletRepository,
            IAdditionalWalletRepository additionalWalletRepository,
            IBlockchainSignFacadeClient blockchainSignFacadeClient)
        {
            _cqrsEngine = cqrsEngine;
            _walletRepository = walletRepository;
            _additionalWalletRepository = additionalWalletRepository;
            _blockchainSignFacadeClient = blockchainSignFacadeClient;
        }

        private async Task<bool> AdditionalWalletExistsAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            return await _additionalWalletRepository.ExistsAsync(integrationLayerId, assetId, clientId);
        }


        public async Task<string> CreateWalletAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            var wallet = await _blockchainSignFacadeClient.CreateWalletAsync(integrationLayerId);
            var address = wallet.PublicAddress;
            var command = new BeginBalanceMonitoringCommand
            {
                Address = address,
                AssetId = assetId,
                IntegrationLayerId = integrationLayerId
            };

            await _walletRepository.AddAsync(integrationLayerId, assetId, clientId, address);

            _cqrsEngine.SendCommand
            (
                command,
                BlockchainWalletsBoundedContext.Name,
                BlockchainWalletsBoundedContext.Name
            );

            return address;
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

        public async Task<string> GetDefaultAddressAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            return (await _walletRepository.TryGetAsync(integrationLayerId, assetId, clientId))?.Address;
        }

        public async Task<Guid?> GetClientIdAsync(string integrationLayerId, string assetId, string address)
        {
            return (await _walletRepository.TryGetAsync(integrationLayerId, assetId, address))?.ClientId
                ?? (await _additionalWalletRepository.TryGetAsync(integrationLayerId, assetId, address))?.ClientId;
        }

        public async Task<bool> WalletExistsAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            return await DefaultWalletExistsAsync(integrationLayerId, assetId, clientId)
                || await AdditionalWalletExistsAsync(integrationLayerId, assetId, clientId);
        }

        public async Task<(IEnumerable<IWallet>, string continuationToken)> GetClientWalletsAsync(Guid clientId, int take,
            string continuationToken)
        {
            var (wallets, token) = await _walletRepository.TryGetForClientAsync(clientId, take, continuationToken);

            return (wallets, token);
        }
}
}
