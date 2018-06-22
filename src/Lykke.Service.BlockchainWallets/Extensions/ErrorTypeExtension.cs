using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.BlockchainWallets.ApiContract;
using Lykke.Service.BlockchainWallets.Core.Exceptions;

namespace Lykke.Service.BlockchainWallets.Extensions
{
    public static class ErrorTypeExtension
    {
        public static ErrorCodeType ToErrorCodeType(this ErrorType type)
        {
            switch (type)
            {
                case ErrorType.BaseAddressShouldNotContainSeparator:
                    return ErrorCodeType.BaseAddressShouldNotContainSeparator;

                case ErrorType.BaseAddressAlreadyIncludesExtension:
                    return ErrorCodeType.BaseAddressAlreadyIncludesExtension;

                case ErrorType.BaseAddressIsEmpty:
                    return ErrorCodeType.BaseAddressIsEmpty;

                case ErrorType.ExtensionAddressIsEmpty:
                    return ErrorCodeType.ExtensionAddressIsEmpty;

                case ErrorType.None:
                    return ErrorCodeType.None;

                case ErrorType.RedundantSeparator:
                    return ErrorCodeType.RedundantSeparator;

                case ErrorType.ExtensionAddressShouldNotContainSeparator:
                    return ErrorCodeType.ExtensionAddressShouldNotContainSeparator;

                default:
                    throw new ArgumentOutOfRangeException($"There is no mapping for {type} to ErrorCodeType enum");
            }
        }
    }
}
