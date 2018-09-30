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
    /// -> WalletCreatedEvent
    ///     -> BeginBalanceMonitoringCommand
    ///     -> BeginTransactionHistoryMonitoringCommand
    /// </summary>
    [UsedImplicitly]
    public class WalletSubscriptionSaga
    {
        private readonly IMonitoringSubscriptionRepository _monitoringSubscriptionRepository;

        public WalletSubscriptionSaga(IMonitoringSubscriptionRepository monitoringSubscriptionRepository)
        {
            _monitoringSubscriptionRepository = monitoringSubscriptionRepository;
        }

        [UsedImplicitly]
        private async Task Handle(WalletCreatedEvent evt, ICommandSender sender)
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
    }
}
