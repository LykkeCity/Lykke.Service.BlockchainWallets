using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Contract.Events;
using Lykke.Service.BlockchainWallets.Core;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Workflow.Commands;


namespace Lykke.Service.BlockchainWallets.Workflow.Sagas
{
    /// <summary>
    /// -> WalletCreatedEvent
    ///     -> BeginBalanceMonitoringCommand
    ///     -> BeginTransactionHistoryMonitoringCommand
    /// </summary>
    [UsedImplicitly]
    public class WalletSubscriptionSaga
    {
        private readonly IMonitoringSubscriptionRepository _monitoringSubscriptionRepository;
        private readonly ILog _log;


        public WalletSubscriptionSaga(
            IMonitoringSubscriptionRepository monitoringSubscriptionRepository,
            ILog log)
        {
            _monitoringSubscriptionRepository = monitoringSubscriptionRepository;
            _log = log.CreateComponentScope(nameof(WalletSubscriptionSaga));
        }

        [UsedImplicitly]
        private async Task Handle(WalletCreatedEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(WalletCreatedEvent), evt, "");

            try
            {
                var address = evt.Address;
                var assetId = evt.AssetId;
                var blockchainType = evt.BlockchainType;

                Task<bool> WalletIsSubscribedAsync(MonitoringSubscriptionType subscriptionType)
                {
                    return _monitoringSubscriptionRepository.WalletIsSubscribedAsync
                    (
                        blockchainType: blockchainType,
                        address: address,
                        assetId: assetId,
                        subscriptionType: subscriptionType
                    );
                }

                if (!await WalletIsSubscribedAsync(MonitoringSubscriptionType.Balance))
                {
                    sender.SendCommand
                    (
                        new BeginBalanceMonitoringCommand
                        {
                            Address = address,
                            AssetId = assetId,
                            BlockchainType = blockchainType
                        },
                        BlockchainWalletsBoundedContext.Name
                    );
                }

                if (!await WalletIsSubscribedAsync(MonitoringSubscriptionType.TransactionHistory))
                {
                    sender.SendCommand
                    (
                        new BeginTransactionHistoryMonitoringCommand
                        {
                            Address = address,
                            AssetId = assetId,
                            BlockchainType = blockchainType
                        },
                        BlockchainWalletsBoundedContext.Name
                    );
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(WalletCreatedEvent), evt, ex);

                throw;
            }
        }
    }
}
