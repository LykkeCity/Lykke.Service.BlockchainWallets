using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.BlockchainWallets.Contract.Models;

namespace Lykke.Service.BlockchainWallets.Core.Exceptions
{
    public class CacheIsNotReadyException : Exception
    {
        public CacheIsNotReadyException(string message) : base(message)
        {

        }
    }
}
