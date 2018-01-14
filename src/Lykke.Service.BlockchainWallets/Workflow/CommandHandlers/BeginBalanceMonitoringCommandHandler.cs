using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Service.BlockchainWallets.Core.Domain.Wallet.Commands;
using Lykke.Service.BlockchainWallets.Core.Services;


namespace Lykke.Service.BlockchainWallets.Workflow.CommandHandlers
{
    public class BeginBalanceMonitoringCommandHandler
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly ILog                          _log;


        public BeginBalanceMonitoringCommandHandler(
            IBlockchainIntegrationService blockchainIntegrationService,
            ILog log)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _log                          = log;
        }


        public async Task<CommandHandlingResult> Handle(BeginBalanceMonitoringCommand command, IEventPublisher publisher)
        {
            var apiClient = _blockchainIntegrationService.GetApiClient(command.IntegrationLayerId);

            if (apiClient != null)
            {
                await apiClient.StartBalanceObservationAsync(command.Address);

                return CommandHandlingResult.Ok();
            }
            else
            {
                await _log.WriteWarningAsync
                (
                    component: nameof(BeginBalanceMonitoringCommandHandler),
                    process:   nameof(Handle),
                    context:   command.IntegrationLayerId,
                    info:      "Blockchain integration layer is not supported"
                );

                return CommandHandlingResult.Fail(TimeSpan.FromMinutes(15));
            }
        }
    }
}
