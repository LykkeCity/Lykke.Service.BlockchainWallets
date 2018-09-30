using System.Threading.Tasks;
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


        public WalletUnsubscriptionSaga(
            IMonitoringSubscriptionRepository monitoringSubscriptionRepository)
        {
            _monitoringSubscriptionRepository = monitoringSubscriptionRepository;
        }


        [UsedImplicitly]
        private async Task Handle(WalletDeletedEvent evt, ICommandSender sender)
        {
            var address = evt.Address;
            var assetId = evt.AssetId;
            var blockchainType = evt.BlockchainType ?? evt.IntegrationLayerId;

            Task<bool> WalletIsSubscribedAsync(MonitoringSubscriptionType subscriptionType)
            {
                return _monitoringSubscriptionRepository.WalletIsSubscribedAsync
                (
                    blockchainType: blockchainType,
                    address: address,
                    subscriptionType: subscriptionType
                );
            }

            if (await WalletIsSubscribedAsync(MonitoringSubscriptionType.Balance))
            {
                sender.SendCommand
                (
                    new EndBalanceMonitoringCommand
                    {
                        Address = address,
                        BlockchainType = blockchainType
                    },
                    BlockchainWalletsBoundedContext.Name
                );
            }

            if (await WalletIsSubscribedAsync(MonitoringSubscriptionType.TransactionHistory))
            {
                sender.SendCommand
                (
                    new EndTransactionHistoryMonitoringCommand
                    {
                        Address = address,
                        BlockchainType = blockchainType
                    },
                    BlockchainWalletsBoundedContext.Name
                );
            }
        }

        [UsedImplicitly]
        private async Task Handle(WalletArchivedEvent evt, ICommandSender sender)
        {
            var address = evt.Address;
            var blockchainType = evt.BlockchainType;

            Task<bool> WalletIsSubscribedAsync(MonitoringSubscriptionType subscriptionType)
            {
                return _monitoringSubscriptionRepository.WalletIsSubscribedAsync
                (
                    blockchainType: blockchainType,
                    address: address,
                    subscriptionType: subscriptionType
                );
            }

            if (await WalletIsSubscribedAsync(MonitoringSubscriptionType.Balance))
            {
                sender.SendCommand
                (
                    new EndBalanceMonitoringCommand
                    {
                        Address = address,
                        BlockchainType = blockchainType
                    },
                    BlockchainWalletsBoundedContext.Name
                );
            }

            if (await WalletIsSubscribedAsync(MonitoringSubscriptionType.TransactionHistory))
            {
                sender.SendCommand
                (
                    new EndTransactionHistoryMonitoringCommand
                    {
                        Address = address,
                        BlockchainType = blockchainType
                    },
                    BlockchainWalletsBoundedContext.Name
                );
            }
        }
    }
}
