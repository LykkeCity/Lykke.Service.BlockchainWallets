using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.BlockchainWallets.Core.Exceptions
{
    public enum ErrorType
    {
        None = 0,
        BaseAddressShouldNotContainSeparator = 1,
        BaseAddressIsEmpty = 2,
        ExtensionAddressShouldNotContainSeparator = 3
    }
}
