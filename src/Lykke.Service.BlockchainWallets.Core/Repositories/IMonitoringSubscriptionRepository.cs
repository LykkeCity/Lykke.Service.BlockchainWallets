using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Core.Repositories
{
    public interface IMonitoringSubscriptionRepository
    {
        Task<bool> AddressIsSubscribedAsync(string blockchainType, string address, MonitoringSubscriptionType subscriptionType);

        Task RegisterWalletSubscriptionAsync(string blockchainType, string address, string assetId, MonitoringSubscriptionType subscriptionType);

        Task UnregisterWalletSubscriptionAsync(string blockchainType, string address, string assetId, MonitoringSubscriptionType subscriptionType);

        Task<bool> WalletIsSubscribedAsync(string blockchainType, string address, string assetId, MonitoringSubscriptionType subscriptionType);
    }
}
