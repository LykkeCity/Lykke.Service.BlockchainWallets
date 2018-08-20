using System;
using System.Net;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainWallets.Core;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Workflow.Commands;


namespace Lykke.Service.BlockchainWallets.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class BeginTransactionHistoryMonitoringCommandHandler
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly ILog _log;
        private readonly IMonitoringSubscriptionRepository _monitoringSubscriptionRepository;


        public BeginTransactionHistoryMonitoringCommandHandler(
            IBlockchainIntegrationService blockchainIntegrationService,
            IMonitoringSubscriptionRepository monitoringSubscriptionRepository,
            ILogFactory logFactory)
        {
            _blockchainIntegrationService = blockchainIntegrationService ?? throw new ArgumentNullException(nameof(blockchainIntegrationService));
            _monitoringSubscriptionRepository = monitoringSubscriptionRepository ?? throw new ArgumentNullException(nameof(monitoringSubscriptionRepository));
            if (logFactory == null)
                throw new ArgumentNullException(nameof(logFactory));

            _log = logFactory.CreateLog(this);
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(BeginTransactionHistoryMonitoringCommand command, IEventPublisher publisher)
        {
            var apiClient = _blockchainIntegrationService.TryGetApiClient(command.BlockchainType);

            if (apiClient == null)
            {
                throw new NotSupportedException($"Blockchain type [{command.BlockchainType}] is not supported");
            }
         
            
            const MonitoringSubscriptionType subscriptionType = MonitoringSubscriptionType.TransactionHistory;

            var address = command.Address;
            var assetId = command.AssetId;
            var blockchainType = command.BlockchainType;

            if (await _monitoringSubscriptionRepository.WalletSubscriptionsCount(blockchainType, address, subscriptionType) == 0)
            {
                try
                {
                    await apiClient.StartHistoryObservationOfIncomingTransactionsAsync(address);
                }
                catch (ErrorResponseException e) when (e.StatusCode == HttpStatusCode.Conflict)
                {
                    
                }
                catch (ErrorResponseException e)
                {
                    string warningMessage;

                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (e.StatusCode)
                    {
                        case HttpStatusCode.NotImplemented:
                            warningMessage =
                                $"Blockchain type [{blockchainType}] does not support transactions history.";
                            break;
                        case HttpStatusCode.NotFound:
                            warningMessage =
                                $"Blockchain type [{blockchainType}] either does not support transactions history, or not respond.";
                            break;
                        default:
                            throw;
                    }

                    _log.Warning(warningMessage, context: command);

                    return CommandHandlingResult.Ok();
                }
            }
            
            // Register subscription of specified asset for specified wallet
            await _monitoringSubscriptionRepository.RegisterWalletSubscriptionAsync
            (
                blockchainType,
                address,
                assetId,
                subscriptionType
            );

            return CommandHandlingResult.Ok();
        }
    }
}
