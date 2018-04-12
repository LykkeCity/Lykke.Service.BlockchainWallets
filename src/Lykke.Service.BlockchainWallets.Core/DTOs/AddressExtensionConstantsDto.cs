using Lykke.Service.BlockchainWallets.Contract.Constants;

namespace Lykke.Service.BlockchainWallets.Core.DTOs
{
    public class AddressExtensionConstantsDto
    {
        public string DisplayName { get; set; }

        public char Separator { get; set; }

        public AddressExtensionTypeForDeposit TypeForDeposit { get; set; }

        public AddressExtensionTypeForWithdrawal TypeForWithdrawal { get; set; }
    }
}
