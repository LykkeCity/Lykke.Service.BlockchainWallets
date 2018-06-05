using System;

namespace Lykke.Service.BlockchainWallets.Client
{
    public class ResultValidationException : Exception
    {
        public ResultValidationException(string message, Exception innnerException = null) :
            base(message, innnerException)
        {
        }
    }
}
