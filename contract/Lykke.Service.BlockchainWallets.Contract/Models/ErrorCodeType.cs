using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.BlockchainWallets.Contract.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ErrorCodeType
    {
        None = 0,
        BaseAddressShouldNotContainSeparator = 1,
        BaseAddressIsEmpty = 2,
        ExtensionAddressShouldNotContainSeparator = 3
    }
}
