using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.BlockchainWallets.Core.Exceptions
{
    public enum ErrorType
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
