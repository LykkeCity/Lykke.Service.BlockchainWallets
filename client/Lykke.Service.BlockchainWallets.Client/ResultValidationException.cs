using System;

namespace Lykke.Service.BlockchainWallets.Client
{
    public class ResultValidationException : Exception
    {
        public ResultValidationException(string message, Exception innnerException = null) :
            base(message, innnerException)
        {
        }

        public ResultValidationException(string message, object actualValue, Exception innerException = null) :
            base(BuildMessage(message, actualValue), innerException)
        {
        }

        private static string BuildMessage(string message, object actualValue)
        {
            return actualValue == null
                ? $"{message}. Actual value: is null"
                : $"{message}. Actual value: [{actualValue}]";
        }
    }
}
