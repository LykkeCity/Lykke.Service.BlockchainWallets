using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.BlockchainWallets.Contract.Models;

namespace Lykke.Service.BlockchainWallets.Core.Exceptions
{
    public class OperationException : Exception
    {
        public OperationException(string message, OperationErrorCode errorType) : base(message)
        {
            ErrorCode = errorType;
        }

        public OperationErrorCode ErrorCode { get; protected set; }
    }
}
