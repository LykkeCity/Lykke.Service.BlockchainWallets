using JetBrains.Annotations;

namespace Lykke.Service.BlockchainWallets.Contract.Models
{
    [PublicAPI]
    public class AddressExtensionConstantsResponse
    {
        public string AddressExtensionDisplayName { get; set; }

        public string BaseAddressDisplayName { get; set; }

        public AddressExtensionTypeForDeposit TypeForDeposit { get; set; }

        public AddressExtensionTypeForWithdrawal TypeForWithdrawal { get; set; }
    }
}
