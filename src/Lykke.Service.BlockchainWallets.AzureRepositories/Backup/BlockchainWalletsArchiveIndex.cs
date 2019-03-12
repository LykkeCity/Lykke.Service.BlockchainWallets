using System;
using Lykke.AzureStorage.Tables;

namespace Lykke.Service.BlockchainWallets.AzureRepositories.Backup
{
    internal class BlockchainWalletsArchiveIndex : AzureTableEntity
    {
        public static string GetPartitionKey(Guid clientId)
        {
            return clientId.ToString();
        }

        public static string GetRowKey(string address, string integrationLayerId)
        {
            return $"{integrationLayerId}_{address}";
        }
    }
}
