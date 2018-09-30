using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Core.Repositories
{
    public interface IMonitoringSubscriptionRepository
    {
        Task<int> WalletSubscriptionsCount(string blockchainType, string address, MonitoringSubscriptionType subscriptionType);
        
        Task RegisterWalletSubscriptionAsync(string blockchainType, string address, MonitoringSubscriptionType subscriptionType);

        Task UnregisterWalletSubscriptionAsync(string blockchainType, string address, MonitoringSubscriptionType subscriptionType);

        Task<bool> WalletIsSubscribedAsync(string blockchainType, string address, MonitoringSubscriptionType subscriptionType);
    }
}
