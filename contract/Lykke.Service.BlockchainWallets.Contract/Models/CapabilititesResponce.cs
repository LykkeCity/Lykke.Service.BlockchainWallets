using JetBrains.Annotations;

namespace Lykke.Service.BlockchainWallets.Contract.Models
{
    [PublicAPI]
    public class CapabilititesResponce
    {
        public bool IsPublicAddressExtensionRequired { get; set; }
    }
}
