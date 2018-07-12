using JetBrains.Annotations;
using Lykke.AzureStorage.Tables;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;

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
        
        public class FromWalletCredentials : AzureTableEntity, IWalletCredentials
        {
            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string ClientId { get; set; }
        
            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string SolarCoinWalletAddress { get; set; }
            
            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string MultiSig { get; set; }
            
            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string ChronoBankContract { get; set; }
            
            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string QuantaContract { get; set; }
            
            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string ColoredMultiSig { get; set; }

            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string Address { get; set; }

            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string PublicKey { get; set; }

            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string PrivateKey { get; set; }

            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public bool PreventTxDetection { get; set; }

            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string EncodedPrivateKey { get; set; }

            /// <summary>
            /// Conversion wallet is used for accepting BTC deposit and transfering needed LKK amount
            /// </summary>

            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string BtcConvertionWalletPrivateKey { get; set; }

            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string BtcConvertionWalletAddress { get; set; }

            //EthContract in fact. ToDo: rename
            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string EthConversionWalletAddress { get; set; }

            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string EthAddress { get; set; }

            [UsedImplicitly(ImplicitUseKindFlags.Assign)]
            public string EthPublicKey { get; set; }
        }
    }
}
