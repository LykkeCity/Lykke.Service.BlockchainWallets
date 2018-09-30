using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.Common.Log;
using Lykke.Service.BlockchainWallets.Core;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.SettingsReader;


namespace Lykke.Service.BlockchainWallets.AzureRepositories
{
    public class MonitoringSubscriptionRepository : IMonitoringSubscriptionRepository
    {
        private readonly INoSQLTableStorage<MonitoringSubscriptionEntity> _table;

        private MonitoringSubscriptionRepository(
            INoSQLTableStorage<MonitoringSubscriptionEntity> table)
        {
            _table = table;
        }

        public static IMonitoringSubscriptionRepository Create(IReloadingManager<string> connectionString, ILogFactory logFactory)
        {
        
            var table = AzureTableStorage<MonitoringSubscriptionEntity>.Create
            (
                connectionString,
                "MonitoringSubscriptions",
                logFactory
            );
            

            return new MonitoringSubscriptionRepository(table);
        }

        private static string GetPartitionKey(string blockchainType, MonitoringSubscriptionType subscriptionType)
        {
            return $"{subscriptionType.ToString()}-{blockchainType}";
        }

        private static string GetRowKey(string address)
        {
            return address;
        }

        public async Task RegisterWalletSubscriptionAsync(string blockchainType, string address, MonitoringSubscriptionType subscriptionType)
        {
            var entity = new MonitoringSubscriptionEntity
            {
                Address = address,
                BlockchainType = blockchainType,
                SubscriptionType = subscriptionType,

                PartitionKey = GetPartitionKey(blockchainType, subscriptionType),
                RowKey = GetRowKey(address)
            };

            await _table.InsertOrReplaceAsync(entity);
        }

        public async Task UnregisterWalletSubscriptionAsync(string blockchainType, string address, MonitoringSubscriptionType subscriptionType)
        {
            var partitionKey = GetPartitionKey(blockchainType, subscriptionType);
            var rowKey = GetRowKey(address);

            await _table.DeleteIfExistAsync(partitionKey, rowKey);
        }

        public async Task<bool> WalletIsSubscribedAsync(string blockchainType, string address, MonitoringSubscriptionType subscriptionType)
        {
            var partitionKey = GetPartitionKey(blockchainType, subscriptionType);
            var rowKey = GetRowKey(address);

            return await _table.GetDataAsync(partitionKey, rowKey) != null;
        }
        
        public async Task<int> WalletSubscriptionsCount(string blockchainType, string address, MonitoringSubscriptionType subscriptionType)
        {
            var partitionKey = GetPartitionKey(blockchainType, subscriptionType);

            return (await _table.GetDataAsync(partitionKey)).Count();
        }
    }
}
