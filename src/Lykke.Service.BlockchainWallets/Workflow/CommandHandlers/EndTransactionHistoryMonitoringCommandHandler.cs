using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Service.BlockchainWallets.Core;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Workflow.Commands;

namespace Lykke.Service.BlockchainWallets.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class EndTransactionHistoryMonitoringCommandHandler
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly IMonitoringSubscriptionRepository _monitoringSubscriptionRepository;
        private readonly ILog _log;


        public EndTransactionHistoryMonitoringCommandHandler(
            IBlockchainIntegrationService blockchainIntegrationService,
            IMonitoringSubscriptionRepository monitoringSubscriptionRepository,
            ILog log)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _monitoringSubscriptionRepository = monitoringSubscriptionRepository;
            _log = log;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(EndTransactionHistoryMonitoringCommand command, IEventPublisher publisher)
        {
            _log.WriteInfo(nameof(EndTransactionHistoryMonitoringCommand), command, "");

            try
            {
                const MonitoringSubscriptionType subscriptionType = MonitoringSubscriptionType.TransactionHistory;

                var address = command.Address;
                var assetId = command.AssetId;
                var blockchainType = command.BlockchainType;

                var apiClient = _blockchainIntegrationService.TryGetApiClient(blockchainType);

                if (apiClient != null)
                {
                    if (!await _monitoringSubscriptionRepository.AddressIsSubscribedAsync(blockchainType, address, subscriptionType))
                    {
                        await apiClient.StopHistoryObservationOfIncomingTransactionsAsync(address);
                    }
                    
                    await _monitoringSubscriptionRepository.UnregisterWalletSubscriptionAsync
                    (
                        blockchainType: blockchainType,
                        address: address,
                        assetId: assetId,
                        subscriptionType: subscriptionType
                    );

                    return CommandHandlingResult.Ok();
                }

                throw new NotSupportedException($"Blockchain type [{blockchainType}] is not supported");
            }
            catch (Exception e)
            {
                _log.WriteError(nameof(EndTransactionHistoryMonitoringCommand), command, e);

                throw;
            }
        }
    }
}
