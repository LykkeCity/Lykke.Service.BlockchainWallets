using JetBrains.Annotations;

namespace Lykke.Service.BlockchainWallets.Contract.Models
{
    [PublicAPI]
    public class AddressParseResultResponce
    {
        public bool IsPublicAddressExtensionRequired { get; set; }
        
        public string BaseAddress { get; set; }
        
        public string AddressExtension { get; set; }
    }
}
