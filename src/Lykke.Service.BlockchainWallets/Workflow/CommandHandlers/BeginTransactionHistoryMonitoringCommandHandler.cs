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
    public class BeginTransactionHistoryMonitoringCommandHandler
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly ILog _log;


        public BeginTransactionHistoryMonitoringCommandHandler(
            IBlockchainIntegrationService blockchainIntegrationService,
            ILog log)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _log = log;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(BeginTransactionHistoryMonitoringCommand command, IEventPublisher publisher)
        {
            _log.WriteInfo(nameof(BeginTransactionHistoryMonitoringCommand), command, "");

            try
            {
                var apiClient = _blockchainIntegrationService.TryGetApiClient(command.BlockchainType);

                if (apiClient != null)
                {
                    await apiClient.StartBalanceObservationAsync(command.Address);

                    publisher.PublishEvent(new TransactionHistoryMonitoringBeganEvent
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
                _log.WriteError(nameof(BeginTransactionHistoryMonitoringCommand), command, e);

                throw;
            }
        }
    }
}
