namespace Lykke.Service.BlockchainWallets.Contract.Models
{
    public class MergeAddressRequest
    {
        public string AddressExtension { get; set; }

        public string BaseAddress { get; set; }

        public string BlockchainType { get; set; }
    }
}
