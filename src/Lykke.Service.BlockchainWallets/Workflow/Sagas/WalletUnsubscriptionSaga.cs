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
    /// -> WalletDeletedEvent
    ///     -> EndBalanceMonitoringCommand
    ///     -> EndTransactionHistoryMonitoringCommand
    /// </summary>
    [UsedImplicitly]
    public class WalletUnsubscriptionSaga
    {
        private readonly IMonitoringSubscriptionRepository _monitoringSubscriptionRepository;
        private readonly ILog _log;


        public WalletUnsubscriptionSaga(
            IMonitoringSubscriptionRepository monitoringSubscriptionRepository,
            ILog log)
        {
            _monitoringSubscriptionRepository = monitoringSubscriptionRepository;
            _log = log.CreateComponentScope(nameof(WalletUnsubscriptionSaga));
        }


        [UsedImplicitly]
        private async Task Handle(WalletDeletedEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(WalletDeletedEvent), evt, "");

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
                        new EndBalanceMonitoringCommand
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
                        new EndTransactionHistoryMonitoringCommand
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
                _log.WriteError(nameof(WalletDeletedEvent), evt, ex);

                throw;
            }
        }
    }
}
