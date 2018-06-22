using System;
using Lykke.Common.Api.Contract.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.BlockchainWallets.ApiContract
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ErrorCodeType
    {
        None = 0,
        BaseAddressShouldNotContainSeparator = 1,
        BaseAddressAlreadyIncludesExtension = 2,
        BaseAddressIsEmpty = 3,
        ExtensionAddressIsEmpty = 4,
        RedundantSeparator = 5,
        ExtensionAddressShouldNotContainSeparator = 6
    }
}
