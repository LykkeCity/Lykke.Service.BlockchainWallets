namespace Lykke.Service.BlockchainWallets.Core.DTOs
{
    public class AddressParseResultDto
    {
        public bool IsPublicAddressExtensionRequired { get; set; }
        public string BaseAddress { get; set; }

        public string AddressExtension { get; set; }
    }
}
