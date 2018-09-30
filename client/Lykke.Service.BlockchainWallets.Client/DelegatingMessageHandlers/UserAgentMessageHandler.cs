using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Microsoft.Extensions.PlatformAbstractions;

namespace Lykke.Service.BlockchainWallets.Client.DelegatingMessageHandlers
{
    internal class UserAgentMessageHandler : DelegatingHandler
    {
        public UserAgentMessageHandler()
        {
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Add("User-Agent", 
                $"{PlatformServices.Default.Application.ApplicationName}/{PlatformServices.Default.Application.ApplicationVersion}");

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
