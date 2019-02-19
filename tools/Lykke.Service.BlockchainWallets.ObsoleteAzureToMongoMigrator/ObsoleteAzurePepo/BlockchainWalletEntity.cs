using System;
using Common;
using Lykke.AzureStorage.Tables;
using Lykke.Service.BlockchainWallets.Contract;

namespace Lykke.Service.BlockchainWallets.ObsoleteAzureToMongoMigrator.ObsoleteAzurePepo
{
    public class BlockchainWalletEntity : AzureTableEntity
    {
        public static string GetPartitionKey(string integrationLayerId, Guid clientId)
        {
            return $"{integrationLayerId}-{clientId.ToString().CalculateHexHash32(3)}";
        }

        public static string GetRowKey(string address)
        {
            return address;
        }

        public string Address { get; set; }

        public string IntegrationLayerId { get; set; }

        public Guid ClientId { get; set; }

        public CreatorType CreatedBy { get; set; }
    }
}
