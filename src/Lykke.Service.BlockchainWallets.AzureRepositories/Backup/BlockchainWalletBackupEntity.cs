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

        public static string GetRowKey(string address, string blockchainType)
        {
            return $"{blockchainType}_{address}";
        }

        public string Address { get; set; }

        public string BlockchainType { get; set; }

        public Guid ClientId { get; set; }

        public CreatorType CreatedBy { get; set; }
    }
}
