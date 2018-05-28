using System;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables;

namespace Lykke.Service.BlockchainWallets.AzureRepositories
{
    public class BcnCredentialsWalletEntity : AzureTableEntity
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string Address { get; set; }
        
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string AssetId { get; set; }
        
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string ClientId { get; set; }
    }
}
