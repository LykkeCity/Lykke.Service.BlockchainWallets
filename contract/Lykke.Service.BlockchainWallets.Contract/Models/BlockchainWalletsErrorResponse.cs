using System.Collections.Generic;
using Lykke.Common.Api.Contract.Responses;

namespace Lykke.Service.BlockchainWallets.Contract.Models
{
    public class BlockchainWalletsErrorResponse : ErrorResponse
    {
        public ErrorType ErrorCode { get; set; }

        public static BlockchainWalletsErrorResponse Create(string message, ErrorType errorCodeType = ErrorType.None)
        {
            return new BlockchainWalletsErrorResponse()
            {
                ErrorCode = errorCodeType,
                ErrorMessage = message,
                ModelErrors = new Dictionary<string, List<string>>()
            };
        }
    }
}
