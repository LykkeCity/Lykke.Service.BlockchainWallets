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
    public class BeginBalanceMonitoringCommandHandler
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly IMonitoringSubscriptionRepository _monitoringSubscriptionRepository;
        private readonly ILog _log;


        public BeginBalanceMonitoringCommandHandler(
            IBlockchainIntegrationService blockchainIntegrationService,
            IMonitoringSubscriptionRepository monitoringSubscriptionRepository,
            ILog log)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _monitoringSubscriptionRepository = monitoringSubscriptionRepository;
            _log = log;
        }


        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(BeginBalanceMonitoringCommand command, IEventPublisher publisher)
        {
            _log.WriteInfo(nameof(BeginBalanceMonitoringCommand), command, "");

            try
            {
                const MonitoringSubscriptionType subscriptionType = MonitoringSubscriptionType.Balance;

                var address = command.Address;
                var assetId = command.AssetId;
                var blockchainType = command.BlockchainType;

                var apiClient = _blockchainIntegrationService.TryGetApiClient(blockchainType);

                if (apiClient != null)
                {
                    if (!await _monitoringSubscriptionRepository.AddressIsSubscribedAsync(blockchainType, address, subscriptionType))
                    {
                        await apiClient.StartBalanceObservationAsync(address);
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
                
                throw new NotSupportedException($"Blockchain type [{blockchainType}] is not supported");
            }
            catch (Exception e)
            {
                _log.WriteError(nameof(BeginBalanceMonitoringCommand), command, e);

                throw;
            }
        }
    }
}
