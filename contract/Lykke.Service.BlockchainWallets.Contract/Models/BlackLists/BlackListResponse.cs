namespace Lykke.Service.BlockchainWallets.Contract.Models.BlackLists
{
    public class BlackListResponse
    {
        public string BlockchainType { get; set; }

        public string BlockedAddress { get; set; }

        public bool IsCaseSensitive { get; set; }
    }
}
