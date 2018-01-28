using System;
using Common;
using Lykke.AzureStorage.Tables;
using Lykke.Service.BlockchainWallets.Core.Domain.Wallet;

namespace Lykke.Service.BlockchainWallets.AzureRepositories
{
    public class WalletEntity : AzureTableEntity, IWallet
    {
        public static string GetPartitionKey(string integrationLayerId, string assetId, Guid clientId)
        {
            return $"{integrationLayerId}-{assetId}-{clientId.ToString().CalculateHexHash32(3)}";
        }

        public static string GetRowKey(Guid clientId)
        {
            return $"{clientId}";
        }

        public string Address { get; set; }

        public string AssetId { get; set; }

        public string IntegrationLayerId { get; set; }

        public Guid ClientId { get; set; }
    }
}
