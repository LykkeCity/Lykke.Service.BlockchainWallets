using Lykke.Service.BlockchainWallets.Core.FirstGeneration;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.BlockchainWallets.AzureRepositories.FirstGeneration
{
    public class WalletCredentialsHistoryRecord : TableEntity, IWalletCredentials
    {
        public string ClientId { get; set; }
        public string Address { get; set; }
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public string MultiSig { get; set; }
        public string ColoredMultiSig { get; set; }
        public bool PreventTxDetection { get; set; }
        public string EncodedPrivateKey { get; set; }
        public string BtcConvertionWalletPrivateKey { get; set; }
        public string BtcConvertionWalletAddress { get; set; }
        public string EthConversionWalletAddress { get; set; }
        public string EthAddress { get; set; }
        public string EthPublicKey { get; set; }
        public string SolarCoinWalletAddress { get; set; }
        public string ChronoBankContract { get; set; }
        public string QuantaContract { get; set; }

        public static string GeneratePartitionKey(string clientId)
        {
            return clientId;
        }

        public static WalletCredentialsHistoryRecord Create(IWalletCredentials creds)
        {
            return new WalletCredentialsHistoryRecord
            {
                Address = creds.Address,
                ClientId = creds.ClientId,
                ColoredMultiSig = creds.ColoredMultiSig,
                EncodedPrivateKey = creds.EncodedPrivateKey,
                MultiSig = creds.MultiSig,
                PublicKey = creds.PublicKey,
                PrivateKey = creds.PrivateKey,
                PreventTxDetection = creds.PreventTxDetection,
                PartitionKey = GeneratePartitionKey(creds.ClientId),
                BtcConvertionWalletPrivateKey = creds.BtcConvertionWalletPrivateKey,
                BtcConvertionWalletAddress = creds.BtcConvertionWalletAddress,
                EthConversionWalletAddress = creds.EthConversionWalletAddress,
                EthAddress = creds.EthAddress,
                EthPublicKey = creds.EthPublicKey,
                SolarCoinWalletAddress = creds.SolarCoinWalletAddress,
                ChronoBankContract = creds.ChronoBankContract,
                QuantaContract = creds.QuantaContract
            };
        }
    }
}
