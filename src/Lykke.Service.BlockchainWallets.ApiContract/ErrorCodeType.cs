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
        BaseAddressAlreadyIncludesExtension = 1,
        BaseAddressIsEmpty = 2,
        ExtensionAddressIsEmpty = 3,
        RedundantSeparator = 4
    }
}
