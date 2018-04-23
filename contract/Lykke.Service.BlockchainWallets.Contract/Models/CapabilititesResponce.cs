using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.BlockchainWallets.Contract.Models
{
    [PublicAPI]
    public class CapabilititesResponce
    {
        public bool IsPublicAddressExtensionRequired { get; set; }
    }
}
