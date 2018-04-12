using Lykke.Service.BlockchainWallets.Contract.Constants;

namespace Lykke.Service.BlockchainWallets.Models
{
    public class AddressExtensionConstantsResponse
    {
        public string DisplayName { get; set; }

        public AddressExtensionTypeForDeposit TypeForDeposit { get; set; }

        public AddressExtensionTypeForWithdrawal TypeForWithdrawal { get; set; }
    }
}
