using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.BlockchainWallets.Contract
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CreatorType
    {
        LykkeWallet = 1,
        LykkePay = 2
    }
}
