using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Workflow.Commands;
using Lykke.Service.BlockchainWallets.Workflow.Events;

namespace Lykke.Service.BlockchainWallets.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class EndTransactionHistoryMonitoringCommandHandler
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly ILog _log;


        public EndTransactionHistoryMonitoringCommandHandler(
            IBlockchainIntegrationService blockchainIntegrationService,
            ILog log)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _log = log;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(EndTransactionHistoryMonitoringCommand command, IEventPublisher publisher)
        {
            _log.WriteInfo(nameof(EndTransactionHistoryMonitoringCommand), command, "");

            try
            {
                var apiClient = _blockchainIntegrationService.TryGetApiClient(command.BlockchainType);

                if (apiClient != null)
                {
                    await apiClient.StopBalanceObservationAsync(command.Address);

                    publisher.PublishEvent(new BalanceMonitoringEndedEvent
                    {
                        Address = command.Address,
                        AssetId = command.AssetId,
                        BlockchainType = command.BlockchainType
                    });

                    return CommandHandlingResult.Ok();
                }

                throw new NotSupportedException($"Blockchain type [{command.BlockchainType}] is not supported");
            }
            catch (Exception e)
            {
                _log.WriteError(nameof(EndTransactionHistoryMonitoringCommand), command, e);

                throw;
            }
        }
    }
}
