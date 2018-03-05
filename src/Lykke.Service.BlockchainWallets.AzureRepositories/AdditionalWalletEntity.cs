using System;
using Lykke.AzureStorage.Tables;
using Lykke.Service.BlockchainWallets.Core.Domain.Wallet;

namespace Lykke.Service.BlockchainWallets.AzureRepositories
{
    public class AdditionalWalletEntity : AzureTableEntity, IWallet
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
