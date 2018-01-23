using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Service.BlockchainWallets.Contract.Events;
using Lykke.Service.BlockchainWallets.Core.Domain.Wallet.Commands;
using Lykke.Service.BlockchainWallets.Core.Services;


namespace Lykke.Service.BlockchainWallets.Workflow.CommandHandlers
{
    public class EndBalanceMonitoringCommandHandler
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly ILog                          _log;


        public EndBalanceMonitoringCommandHandler(
            IBlockchainIntegrationService blockchainIntegrationService,
            ILog log)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _log                          = log;
        }


        public async Task<CommandHandlingResult> Handle(EndBalanceMonitoringCommand command, IEventPublisher publisher)
        {
            var apiClient = _blockchainIntegrationService.TryGetApiClient(command.IntegrationLayerId);

            if (apiClient != null)
            {
                await apiClient.StopBalanceObservationAsync(command.Address);

                publisher.PublishEvent(new WalletDeletedEvent
                {
                    Address            = command.Address,
                    AssetId            = command.AssetId,
                    IntegrationLayerId = command.IntegrationLayerId
                });

                return CommandHandlingResult.Ok();
            }
            else
            {
                throw new NotSupportedException($"Blockchain integration layer [{command.IntegrationLayerId}] is not supported");
            }
        }
    }
}
