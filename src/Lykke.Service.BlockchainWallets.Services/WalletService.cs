using System;
using System.Threading.Tasks;
using Lykke.Cqrs;
using Lykke.Service.BlockchainWallets.Core.Domain.Wallet;
using Lykke.Service.BlockchainWallets.Core.Domain.Wallet.Commands;
using Lykke.Service.BlockchainWallets.Core.Services;

namespace Lykke.Service.BlockchainWallets.Services
{
    public class WalletService : IWalletService
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly ICqrsEngine                   _cqrsEngine;
        private readonly IWalletRepository             _walletRepository;


        public WalletService(
            ICqrsEngine cqrsEngine,
            IBlockchainIntegrationService blockchainIntegrationService,
            IWalletRepository walletRepository)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _cqrsEngine                   = cqrsEngine;
            _walletRepository             = walletRepository;
        }


        public async Task<string> CreateWalletAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            var signServiceClient = _blockchainIntegrationService.GetSignServiceClient(integrationLayerId);
            if (signServiceClient == null)
            {
                throw new NotSupportedException($"Blockchain integration [{integrationLayerId}] is not suppoerted.");
            }
            

            var wallet  = await signServiceClient.CreateWalletAsync();
            var address = wallet.PublicAddress;
            var command = new BeginBalanceMonitoringCommand
            {
                Address            = address,
                AssetId            = assetId,
                IntegrationLayerId = integrationLayerId
            };

            await _walletRepository.AddAsync(integrationLayerId, assetId, clientId, address);

            _cqrsEngine.SendCommand
            (
                command:              command,
                boundedContext:       WalletsBoundedContext.Name,
                remoteBoundedContext: WalletsBoundedContext.Name
            );
            
            return address;
        }

        public async Task DeleteWalletAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            var wallet  = await _walletRepository.GetAsync(integrationLayerId, assetId, clientId);
            var address = wallet.Address;
            var command = new EndBalanceMonitoringCommand
            {
                Address            = address,
                AssetId            = assetId,
                IntegrationLayerId = integrationLayerId
            };
            
            await _walletRepository.DeleteIfExistsAsync(integrationLayerId, assetId, clientId);

            _cqrsEngine.SendCommand
            (
                command:              command,
                boundedContext:       WalletsBoundedContext.Name,
                remoteBoundedContext: WalletsBoundedContext.Name
            );
        }

        public async Task<Guid?> GetClientIdAsync(string integrationLayerId, string assetId, string address)
            => (await _walletRepository.GetAsync(integrationLayerId, assetId, address))?.ClientId;

        public async Task<bool> WalletExistsAsync(string integrationLayerId, string assetId, Guid clientId)
            => await _walletRepository.ExistsAsync(integrationLayerId, assetId, clientId);
    }
}
