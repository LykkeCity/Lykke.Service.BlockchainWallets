using Lykke.Service.BlockchainWallets.Contract;

namespace Lykke.Service.BlockchainWallets.Core.DTOs
{
    public class AddressExtensionConstantsDto
    {
        public string AddressExtensionDisplayName { get; set; }

        public string BaseAddressDisplayName { get; set; }

        public char Separator { get; set; }

        public AddressExtensionTypeForDeposit TypeForDeposit { get; set; }

        public AddressExtensionTypeForWithdrawal TypeForWithdrawal { get; set; }
    }
}
