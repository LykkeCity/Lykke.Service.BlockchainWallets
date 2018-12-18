using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.BlockchainWallets.Contract.Models
{
    [PublicAPI]
    public class AddressExtensionConstantsResponse
    {
        public string Separator { get; set; }

        public string AddressExtensionDisplayName { get; set; }

        public string BaseAddressDisplayName { get; set; }

        public IEnumerable<char> ProhibitedSymbolsForBaseAddress { get; set; }

        public IEnumerable<char> ProhibitedSymbolsForAddressExtension { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public AddressExtensionTypeForDeposit TypeForDeposit { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public AddressExtensionTypeForWithdrawal TypeForWithdrawal { get; set; }
    }
}
