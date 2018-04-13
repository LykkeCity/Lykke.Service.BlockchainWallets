namespace Lykke.Service.BlockchainWallets.Client.Models
{
    public class MergeAddressRequest
    {
        public string Address { get; set; }

        public string AddressExtension { get; set; }

        public string BlockchainType { get; set; }
    }
}
