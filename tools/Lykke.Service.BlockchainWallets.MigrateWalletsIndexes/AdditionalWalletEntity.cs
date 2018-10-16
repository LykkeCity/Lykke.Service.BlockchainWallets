using System;
using Lykke.AzureStorage.Tables;

namespace Lykke.Service.BlockchainWallets.MigrateWalletsIndexes
{
    public class AdditionalWalletEntity : AzureTableEntity
    {
        public static string GetPartitionKey(string integrationLayerId, string assetId, Guid clientId)
        {
            return $"{integrationLayerId}-{assetId}-{clientId.ToString()}";
        }

        public static string GetRowKey(string address)
        {
            return $"{address}";
        }

        public string Address { get; set; }

        public string AssetId { get; set; }

        public string IntegrationLayerId { get; set; }

        public Guid ClientId { get; set; }
    }
}
