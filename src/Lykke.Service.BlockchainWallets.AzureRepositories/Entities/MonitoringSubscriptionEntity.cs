using JetBrains.Annotations;
using Lykke.AzureStorage.Tables;
using Lykke.Service.BlockchainWallets.Core;

namespace Lykke.Service.BlockchainWallets.AzureRepositories
{
    public class MonitoringSubscriptionEntity : AzureTableEntity
    {
        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        public string Address { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        public string AssetId { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        public string BlockchainType { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        public MonitoringSubscriptionType SubscriptionType { get; set; }
    }
}
