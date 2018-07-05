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
    public class BeginBalanceMonitoringCommandHandler
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly IMonitoringSubscriptionRepository _monitoringSubscriptionRepository;


        public BeginBalanceMonitoringCommandHandler(
            IBlockchainIntegrationService blockchainIntegrationService,
            IMonitoringSubscriptionRepository monitoringSubscriptionRepository)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _monitoringSubscriptionRepository = monitoringSubscriptionRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(BeginBalanceMonitoringCommand command, IEventPublisher publisher)
        {
            var apiClient = _blockchainIntegrationService.TryGetApiClient(command.BlockchainType);
            
            if (apiClient == null)
            {
                throw new NotSupportedException($"Blockchain type [{command.BlockchainType}] is not supported.");
            }
            
            const MonitoringSubscriptionType subscriptionType = MonitoringSubscriptionType.Balance;

            var address = command.Address;
            var assetId = command.AssetId;
            var blockchainType = command.BlockchainType;

            if (await _monitoringSubscriptionRepository.WalletSubscriptionsCount(blockchainType, address, subscriptionType) == 0)
            {
                try
                {
                    await apiClient.StartBalanceObservationAsync(address);
                }
                catch (ErrorResponseException e) when (e.StatusCode == HttpStatusCode.Conflict)
                {
                    
                }
            }
                    
            await _monitoringSubscriptionRepository.RegisterWalletSubscriptionAsync
            (
                blockchainType: blockchainType,
                address: address,
                assetId: assetId,
                subscriptionType: subscriptionType
            );
                    
            return CommandHandlingResult.Ok();

        }
    }
}
