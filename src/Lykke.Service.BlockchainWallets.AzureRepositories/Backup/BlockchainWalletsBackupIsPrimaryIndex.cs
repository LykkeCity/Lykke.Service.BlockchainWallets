using System;
using Lykke.AzureStorage.Tables;

namespace Lykke.Service.BlockchainWallets.AzureRepositories.Backup
{
    internal class BlockchainWalletsBackupIsPrimaryIndex:AzureTableEntity
    {
        public static string GetPartitionKey(Guid clientId)
        {
            return clientId.ToString();
        }

        public static string GetRowKey(string integrationLayerId)
        {
            return integrationLayerId;
        }

        public string Address { get; set; }
    }
}
