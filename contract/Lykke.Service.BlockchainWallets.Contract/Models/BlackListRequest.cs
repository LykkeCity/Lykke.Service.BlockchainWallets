namespace Lykke.Service.BlockchainWallets.Contract.Models
{
    public class BlackListRequest
    {
        public string BlockchainType { get; set; }

        public string BlockedAddress { get; set; }

        public bool IsCaseSensitive { get; set; }
    }
}
