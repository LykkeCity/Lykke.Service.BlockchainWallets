using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.BlockchainWallets.Contract.Models;
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

                case ErrorType.BaseAddressIsEmpty:
                    return ErrorCodeType.BaseAddressIsEmpty;

                case ErrorType.None:
                    return ErrorCodeType.None;

                case ErrorType.ExtensionAddressShouldNotContainSeparator:
                    return ErrorCodeType.ExtensionAddressShouldNotContainSeparator;

                default:
                    throw new ArgumentOutOfRangeException($"There is no mapping for {type} to ErrorCodeType enum");
            }
        }
    }
}
