using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.BlockchainWallets.Core.Exceptions
{
    public class OperationException : Exception
    {
        public OperationException(string message, ErrorType errorType) : base(message)
        {
            ErrorCode = errorType;
        }

        public ErrorType ErrorCode { get; protected set; }
    }
}
