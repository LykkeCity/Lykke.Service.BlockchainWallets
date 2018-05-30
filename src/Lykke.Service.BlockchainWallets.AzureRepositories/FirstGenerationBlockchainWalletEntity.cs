using JetBrains.Annotations;
using Lykke.AzureStorage.Tables;

namespace Lykke.Service.BlockchainWallets.AzureRepositories
{
    public static class FirstGenerationBlockchainWalletEntity
    {
        public class FromBcnClientCredentials : AzureTableEntity
        {
            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string Address { get; set; }
            
            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string AssetAddress { get; set; }
        
            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string AssetId { get; set; }
        
            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string ClientId { get; set; }
        }
        
        public class FromWalletCredentials : AzureTableEntity
        {
            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string ClientId { get; set; }
        
            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string SolarCoinWalletAddress { get; set; }
        }
    }
}
