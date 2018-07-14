namespace Lykke.Service.BlockchainWallets.Core.FirstGeneration
{
    public class WalletApiSettings
    {
        public NetworkType NetworkType { get; set; }
    }

    public enum NetworkType
    {
        Main,
        Testnet
    }
}
