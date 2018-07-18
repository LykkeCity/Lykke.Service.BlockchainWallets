using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.BlockchainWallets.Contract.Models;
using Lykke.Service.BlockchainWallets.Core.Exceptions;

namespace Lykke.Service.BlockchainWallets.Extensions
{
    public static class OperationErrorCodeExtension
    {
        public static ErrorType ToErrorCodeType(this OperationErrorCode type)
        {
            switch (type)
            {
                case OperationErrorCode.BaseAddressShouldNotContainSeparator:
                    return ErrorType.BaseAddressShouldNotContainSeparator;

                case OperationErrorCode.BaseAddressIsEmpty:
                    return ErrorType.BaseAddressIsEmpty;

                case OperationErrorCode.None:
                    return ErrorType.None;

                case OperationErrorCode.ExtensionAddressShouldNotContainSeparator:
                    return ErrorType.ExtensionAddressShouldNotContainSeparator;

                default:
                    throw new ArgumentOutOfRangeException($"There is no mapping for {type} to ErrorCodeType enum");
            }
        }
    }
}
