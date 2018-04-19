using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Service.BlockchainWallets.Contract.Events;
using Lykke.Service.BlockchainWallets.Core.Commands;
using Lykke.Service.BlockchainWallets.Core.Services;

namespace Lykke.Service.BlockchainWallets.Workflow.CommandHandlers
{
    public class BeginBalanceMonitoringCommandHandler
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly ILog _log;


        public BeginBalanceMonitoringCommandHandler(
            IBlockchainIntegrationService blockchainIntegrationService,
            ILog log)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _log = log;
        }


        public async Task<CommandHandlingResult> Handle(BeginBalanceMonitoringCommand command, IEventPublisher publisher)
        {
            _log.WriteInfo(nameof(BeginBalanceMonitoringCommand), command, "");

            try
            {
                var apiClient = _blockchainIntegrationService.TryGetApiClient(command.IntegrationLayerId);

                if (apiClient != null)
                {
                    await apiClient.StartBalanceObservationAsync(command.Address);

                    publisher.PublishEvent(new WalletCreatedEvent
                    {
                        Address = command.Address,
                        AssetId = command.AssetId,
                        IntegrationLayerId = command.IntegrationLayerId
                    });

                    return CommandHandlingResult.Ok();
                }

                throw new NotSupportedException($"Blockchain integration layer [{command.IntegrationLayerId}] is not supported");
            }
            catch (Exception e)
            {
                _log.WriteError(nameof(BeginBalanceMonitoringCommand), command, e);

                throw;
            }
        }
    }
}
