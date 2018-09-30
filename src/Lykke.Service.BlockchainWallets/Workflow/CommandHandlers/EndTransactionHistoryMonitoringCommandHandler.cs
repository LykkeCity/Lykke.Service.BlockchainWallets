using System;
using System.Net;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Service.BlockchainApi.Client;
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


        public EndTransactionHistoryMonitoringCommandHandler(
            IBlockchainIntegrationService blockchainIntegrationService,
            IMonitoringSubscriptionRepository monitoringSubscriptionRepository)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _monitoringSubscriptionRepository = monitoringSubscriptionRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(EndTransactionHistoryMonitoringCommand command, IEventPublisher publisher)
        {
            var apiClient = _blockchainIntegrationService.TryGetApiClient(command.BlockchainType);

            if (apiClient == null)
            {
                throw new NotSupportedException($"Blockchain type [{command.BlockchainType}] is not supportedю");
            }
            
            
            const MonitoringSubscriptionType subscriptionType = MonitoringSubscriptionType.TransactionHistory;

            var address = command.Address;
            var blockchainType = command.BlockchainType;

            
            await _monitoringSubscriptionRepository.UnregisterWalletSubscriptionAsync
            (
                blockchainType: blockchainType,
                address: address,
                subscriptionType: subscriptionType
            );
            
            // TODO: Fix potential issue with subscription/unsubscription race conditions
            // If we have no subscriptions for address-asset pairs for specified address...
            if (await _monitoringSubscriptionRepository.WalletSubscriptionsCount(blockchainType, address, subscriptionType) == 0)
            {
                try
                {
                    // Unsubscribe from address transactions observation (for all assets)
                    await apiClient.StopHistoryObservationOfIncomingTransactionsAsync(address);
                }
                catch (ErrorResponseException e) when (e.StatusCode == HttpStatusCode.NoContent)
                {
                    
                }
            }
               
            return CommandHandlingResult.Ok();

        }
    }
}
