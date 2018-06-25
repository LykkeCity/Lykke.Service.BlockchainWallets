using System.Collections.Generic;
using Lykke.Common.Api.Contract.Responses;

namespace Lykke.Service.BlockchainWallets.Contract.Models
{
    public class BlockchainWalletsErrorResponse : ErrorResponse
    {
        public ErrorCodeType CodeType { get; set; }

        public static BlockchainWalletsErrorResponse Create(string message, ErrorCodeType errorCodeType = ErrorCodeType.None)
        {
            return new BlockchainWalletsErrorResponse()
            {
                CodeType = errorCodeType,
                ErrorMessage = message,
                ModelErrors = new Dictionary<string, List<string>>()
            };
        }
    }
}
