using System;
using Lykke.AzureStorage.Tables;
using Lykke.Service.BlockchainWallets.Contract;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.BlockchainWallets.AzureRepositories.Backup
{
    internal class BlockchainWalletBackupEntity: AzureTableEntity
    {
        public static string GetPartitionKey(Guid clientId)
        {
            return clientId.ToString();
        }

        public static string GetRowKey(string address, string integrationLayerId)
        {
            return $"{integrationLayerId}_{address}";
        }

        public string Address { get; set; }

        public string IntegrationLayerId { get; set; }

        public Guid ClientId { get; set; }

        public CreatorType CreatedBy { get; set; }
    }
}
