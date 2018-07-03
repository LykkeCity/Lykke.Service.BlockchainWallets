using System;
using System.Net;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainWallets.Contract.Models;
using Refit;

namespace Lykke.Service.BlockchainWallets.Client
{
    public class ErrorResponseException : Exception
    {
        public ErrorResponseException(BlockchainWalletsErrorResponse error, ApiException inner) :
            base(error.GetSummaryMessage() ?? string.Empty, inner)
        {
            Error = error;
            StatusCode = inner.StatusCode;
        }

        public BlockchainWalletsErrorResponse Error { get; }

        public HttpStatusCode StatusCode { get; }

    }
}
