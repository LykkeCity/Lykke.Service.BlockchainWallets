namespace Lykke.Service.BlockchainWallets.Core.FirstGeneration
{
    public class SubmitKeysModel
    {
        public string AssetId { get; set; }
        public BcnWallet BcnWallet { get; set; }
    }

    public class BcnWallet
    {
        public string Address { get; set; }
        public string EncodedKey { get; set; }
        public string PublicKey { get; set; }
    }
}
