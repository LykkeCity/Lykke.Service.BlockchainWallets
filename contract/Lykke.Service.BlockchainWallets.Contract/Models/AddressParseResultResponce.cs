namespace Lykke.Service.BlockchainWallets.Contract.Models
{
    public class AddressParseResultResponce
    {
        public bool IsPublicAddressExtensionRequired { get; set; }
        public string BaseAddress { get; set; }

        public string AddressExtension { get; set; }
    }
}
