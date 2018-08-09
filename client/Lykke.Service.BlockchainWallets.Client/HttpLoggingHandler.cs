using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;

namespace Lykke.Service.BlockchainWallets.Client
{
    internal class HttpErrorLoggingHandler : DelegatingHandler
    {
        private readonly ILog _log;

        [Obsolete("Please, use the overload which consumes ILogFactory instead.")]
        public HttpErrorLoggingHandler(ILog log, HttpMessageHandler innerHandler = null)
            : base(innerHandler ?? new HttpClientHandler())
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public HttpErrorLoggingHandler(ILogFactory logFactory, HttpMessageHandler innerHandler = null)
            : base(innerHandler ?? new HttpClientHandler())
        {
            if (logFactory == null)
                throw new ArgumentNullException(nameof(logFactory));
            _log = logFactory.CreateLog(nameof(HttpErrorLoggingHandler));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var id = Guid.NewGuid();

            if (!response.IsSuccessStatusCode)
            {
                await LogRequestAsync(request, id);
                await LogResponseAsync(response, id);
            }

            return response;
        }

        private static bool IsTextBasedContentType(HttpHeaders headers)
        {
            string[] types = {"html", "text", "xml", "json", "txt", "x-www-form-urlencoded"};

            if (!headers.TryGetValues("Content-Type", out var values))
            {
                return false;
            }

            var header = string.Join(" ", values).ToLowerInvariant();

            return types.Any(t => header.Contains(t));
        }

        private async Task LogRequestAsync(HttpRequestMessage request, Guid id)
        {
            var message = new StringBuilder();

            message.AppendLine($"Request {id}: {request.Method.ToString().ToUpper()} {request.RequestUri}");

            foreach (var header in request.Headers)
            {
                message.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            if (request.Content != null)
            {
                foreach (var header in request.Content.Headers)
                {
                    message.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }

                if (request.Content is StringContent ||
                    IsTextBasedContentType(request.Headers) ||
                    IsTextBasedContentType(request.Content.Headers))
                {
                    var content = await request.Content.ReadAsStringAsync();

                    message.AppendLine(content);
                }
            }

            _log.Warning("HTTP API request -> Response status is non success", context: message.ToString());
        }

        private async Task LogResponseAsync(HttpResponseMessage response, Guid id)
        {
            var message = new StringBuilder();

            message.AppendLine(
                $"Response {id}: {(int) response.StatusCode} {response.StatusCode} - {response.ReasonPhrase}");

            foreach (var header in response.Headers)
            {
                message.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            if (response.Content != null)
            {
                foreach (var header in response.Content.Headers)
                {
                    message.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }

                if (response.Content is StringContent ||
                    IsTextBasedContentType(response.Headers) ||
                    IsTextBasedContentType(response.Content.Headers))
                {
                    var content = await response.Content.ReadAsStringAsync();

                    message.AppendLine(content);
                }
            }

            _log.Warning("HTTP API response <- Response status is non success", context: message.ToString());
        }
    }
}
