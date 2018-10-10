using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Tests.Common.Utils;

namespace Lykke.Service.BlockchainWallets.Tests.Common.DelegatingMessageHandlers
{
    //Used in tests only to redirect http requests to the test fixture server
    public class RequestInterceptorHandler : DelegatingHandler
    {
        private readonly HttpClient _httpClient;

        public RequestInterceptorHandler(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var clonedRequest = await request.CloneAsync();
            var response = await _httpClient.SendAsync(clonedRequest, cancellationToken);

            return response;
        }
    }
}
