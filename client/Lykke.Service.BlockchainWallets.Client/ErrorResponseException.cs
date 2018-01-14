using System;
using System.Net;
using Lykke.Common.Api.Contract.Responses;
using Refit;

namespace Lykke.Service.BlockchainWallets.Client
{
    public class ErrorResponseException : Exception
    {
        public ErrorResponseException(ErrorResponse error, ApiException inner) :
            base(error.GetSummaryMessage() ?? string.Empty, inner)
        {
            Error      = error;
            StatusCode = inner.StatusCode;
        }

        public ErrorResponse Error { get; }

        public HttpStatusCode StatusCode { get; }
    }
}
