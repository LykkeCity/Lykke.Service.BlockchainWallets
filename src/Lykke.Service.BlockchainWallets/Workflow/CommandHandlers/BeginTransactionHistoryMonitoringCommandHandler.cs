using System;
using System.Net;
using System.Threading.Tasks;
using Common.Log;
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
    public class BeginTransactionHistoryMonitoringCommandHandler
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly IMonitoringSubscriptionRepository _monitoringSubscriptionRepository;
        private readonly ILog _log;


        public BeginTransactionHistoryMonitoringCommandHandler(
            IBlockchainIntegrationService blockchainIntegrationService,
            IMonitoringSubscriptionRepository monitoringSubscriptionRepository,
            ILog log)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _monitoringSubscriptionRepository = monitoringSubscriptionRepository;
            _log = log;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(BeginTransactionHistoryMonitoringCommand command, IEventPublisher publisher)
        {
            _log.WriteInfo(nameof(BeginTransactionHistoryMonitoringCommand), command, "");

            try
            {
                const MonitoringSubscriptionType subscriptionType = MonitoringSubscriptionType.TransactionHistory;

                var address = command.Address;
                var assetId = command.AssetId;
                var blockchainType = command.BlockchainType;

                var apiClient = _blockchainIntegrationService.TryGetApiClient(command.BlockchainType);

                if (apiClient != null)
                {
                    if (!await _monitoringSubscriptionRepository.AddressIsSubscribedAsync(blockchainType, address, subscriptionType))
                    {
                        try
                        {
                            await apiClient.StartHistoryObservationOfIncomingTransactionsAsync(address);
                        }
                        catch (ErrorResponseException e) when (e.InnerException is Refit.ApiException)
                        {
                            var apiException = (Refit.ApiException) e.InnerException;

                            if (apiException.StatusCode == HttpStatusCode.NotImplemented)
                            {
                                _log.WriteWarning
                                (
                                    nameof(BeginTransactionHistoryMonitoringCommand),
                                    command,
                                    $"Blockchain type [{blockchainType}] does not support transactions history"
                                );
                            }
                            else if (apiException.StatusCode == HttpStatusCode.NotFound)
                            {
                                _log.WriteWarning
                                (
                                    nameof(BeginTransactionHistoryMonitoringCommand),
                                    command,
                                    $"Blockchain type [{blockchainType}] either does not support transactions history, or not respond"
                                );
                            }
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

                throw new NotSupportedException($"Blockchain type [{blockchainType}] is not supported");
            }
            catch (Exception e)
            {
                _log.WriteError(nameof(BeginTransactionHistoryMonitoringCommand), command, e);

                throw;
            }
        }
    }
}
