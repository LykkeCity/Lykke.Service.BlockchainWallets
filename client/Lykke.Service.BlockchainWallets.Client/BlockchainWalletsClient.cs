using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainWallets.Client.Models;
using Microsoft.Extensions.PlatformAbstractions;
using Refit;

namespace Lykke.Service.BlockchainWallets.Client
{
    public class BlockchainWalletsClient : IBlockchainWalletsClient, IDisposable
    {
        private readonly IBlockchainWalletsApi _api;
        private readonly ApiRunner _apiRunner;
        private readonly HttpClient _httpClient;


        public BlockchainWalletsClient(string hostUrl, ILog log, int retriesCount = 5)
        {
            HostUrl = hostUrl ?? throw new ArgumentNullException(nameof(hostUrl));


            _httpClient = new HttpClient(new HttpErrorLoggingHandler(log))
            {
                BaseAddress = new Uri(hostUrl),
                DefaultRequestHeaders =
                {
                    {
                        "User-Agent",
                        $"{PlatformServices.Default.Application.ApplicationName}/{PlatformServices.Default.Application.ApplicationVersion}"
                    }
                }
            };

            _api = RestService.For<IBlockchainWalletsApi>(_httpClient);
            _apiRunner = new ApiRunner(retriesCount);
        }

        /// <summary>
        ///    This constructor intended for testing purposes only.
        /// </summary>
        /// <param name="httpClient">
        ///    Instance of mockable httpClient.
        /// </param>
        internal BlockchainWalletsClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _api = RestService.For<IBlockchainWalletsApi>(_httpClient);
            _apiRunner = new ApiRunner(1);
        }

        private static void ValidateInputParameters(string integrationLayerId, string assetId)
        {
            if (string.IsNullOrEmpty(integrationLayerId))
            {
                throw new ArgumentException(nameof(integrationLayerId));
            }

            if (string.IsNullOrEmpty(assetId))
            {
                throw new ArgumentException(nameof(assetId));
            }
        }

        private static void ValidateInputParameters(string integrationLayerId, string assetId, string address)
        {
            ValidateInputParameters(integrationLayerId, assetId);

            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException(nameof(address));
            }
        }

        private static void ValidateInputParameters(string integrationLayerId, string assetId, Guid clientId)
        {
            ValidateInputParameters(integrationLayerId, assetId);

            if (clientId == Guid.Empty)
            {
                throw new ArgumentException(nameof(clientId));
            }
        }


        /// <inheritdoc />
        public string HostUrl { get; }


        /// <inheritdoc />
        public async Task<IsAliveResponse> GetIsAliveAsync()
        {
            return await _apiRunner.RunAsync(() => _api.GetIsAliveAsync());
        }

        /// <inheritdoc />
        public async Task<string> CreateWalletAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            ValidateInputParameters(integrationLayerId, assetId, clientId);

            try
            {
                var newWallet = await _apiRunner.RunWithRetriesAsync(() => _api.CreateWallet
                (
                    integrationLayerId,
                    assetId,
                    clientId
                ));

                return newWallet.Address;
            }
            catch (ErrorResponseException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteWalletAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            ValidateInputParameters(integrationLayerId, assetId, clientId);

            try
            {
                await _apiRunner.RunWithRetriesAsync(() => _api.DeleteWallet
                (
                    integrationLayerId,
                    assetId,
                    clientId
                ));

                return true;
            }
            catch (ErrorResponseException ex) when (ex.StatusCode == HttpStatusCode.NotFound && ex.Error != null)
            {
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<string> GetAddressAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            var address = await TryGetAddressAsync(integrationLayerId, assetId, clientId);

            if (string.IsNullOrEmpty(address))
            {
                throw new ResultValidationException("Address not found.");
            }

            return address;
        }

        /// <inheritdoc />
        public async Task<Guid> GetClientIdAsync(string integrationLayerId, string assetId, string address)
        {
            var clientId = await TryGetClientIdAsync(integrationLayerId, assetId, address);

            if (!clientId.HasValue)
            {
                throw new ResultValidationException("Client not found.");
            }

            return clientId.Value;
        }

        /// <inheritdoc />
        public async Task<string> TryGetAddressAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            ValidateInputParameters(integrationLayerId, assetId, clientId);

            var address = await _apiRunner.RunWithRetriesAsync(() => _api.GetAddress
            (
                integrationLayerId,
                assetId,
                clientId
            ));

            return address?.Address;
        }

        /// <inheritdoc />
        public async Task<Guid?> TryGetClientIdAsync(string integrationLayerId, string assetId, string address)
        {
            ValidateInputParameters(integrationLayerId, assetId, address);

            var clientId = await _apiRunner.RunWithRetriesAsync(() => _api.GetClientId
            (
                integrationLayerId,
                assetId,
                address
            ));

            return clientId?.ClientId;
        }

        /// <inheritdoc />
        public async Task<ClientWalletsResponse> TryGetClientWalletsAsync(Guid clientId, int take, string continuationToken)
        {
            if (clientId == Guid.Empty)
            {
                throw new ArgumentException(nameof(clientId));
            }

            var response = await _apiRunner.RunWithRetriesAsync(() => _api.GetClientWalletsAsync(clientId, take, continuationToken));

            return response;
        }

        public async Task<IEnumerable<ClientWalletResponse>> TryGetAllClientWalletsAsync(Guid clientId, int batchSize = 50)
        {
            if (clientId == Guid.Empty)
            {
                throw new ArgumentException(nameof(clientId));
            }

            string continuationToken = null;
            List<ClientWalletResponse> wallets = new List<ClientWalletResponse>();

            do
            {
                ClientWalletsResponse response = await TryGetClientWalletsAsync(clientId, batchSize, continuationToken);
                continuationToken = response?.ContinuationToken;

                if (response?.Wallets != null &&
                    response.Wallets.Count() != 0)
                {
                    wallets.AddRange(response.Wallets);
                }
            } while (!string.IsNullOrEmpty(continuationToken));

            return wallets;
        }


        /// <inheritdoc />
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
