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
    public class EndBalanceMonitoringCommandHandler
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly IMonitoringSubscriptionRepository _monitoringSubscriptionRepository;


        public EndBalanceMonitoringCommandHandler(
            IBlockchainIntegrationService blockchainIntegrationService,
            IMonitoringSubscriptionRepository monitoringSubscriptionRepository)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _monitoringSubscriptionRepository = monitoringSubscriptionRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(EndBalanceMonitoringCommand command, IEventPublisher publisher)
        {
            var apiClient = _blockchainIntegrationService.TryGetApiClient(command.BlockchainType);

            if (apiClient == null)
            {
                throw new NotSupportedException($"Blockchain type [{command.BlockchainType}] is not supported.");
            }
            
            
            const MonitoringSubscriptionType subscriptionType = MonitoringSubscriptionType.Balance;

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
                    // Unsubscribe from address balance observation (for all assets)
                    await apiClient.StopBalanceObservationAsync(address);
                }
                catch (ErrorResponseException e) when (e.StatusCode == HttpStatusCode.NoContent)
                {
                    
                }
            }

            return CommandHandlingResult.Ok();
        }
    }
}
