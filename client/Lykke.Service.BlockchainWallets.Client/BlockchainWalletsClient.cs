using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Contract.Models;
using Microsoft.Extensions.PlatformAbstractions;
using Refit;


namespace Lykke.Service.BlockchainWallets.Client
{
    [PublicAPI]
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


        /// <inheritdoc cref="IBlockchainWalletsClient.HostUrl" />
        public string HostUrl { get; }


        /// <inheritdoc cref="IBlockchainWalletsClient.GetIsAliveAsync" />
        public async Task<IsAliveResponse> GetIsAliveAsync()
        {
            return await _apiRunner.RunAsync(() => _api.GetIsAliveAsync());
        }

        /// <inheritdoc cref="IBlockchainWalletsClient.GetWalletsAsync" />
        public async Task<WalletsResponse> GetWalletsAsync(Guid clientId, int take, string continuationToken)
        {
            if (clientId == Guid.Empty)
            {
                throw new ArgumentException(nameof(clientId));
            }

            var response = await _apiRunner.RunWithRetriesAsync(() => _api.GetWalletsAsync(clientId, take, continuationToken));

            return response;
        }

        /// <inheritdoc cref="IBlockchainWalletsClient.CreateWalletAsync" />
        public async Task<WalletResponse> CreateWalletAsync(string integrationLayerId, string assetId, Guid clientId, WalletType walletType = WalletType.DepositWallet)
        {
            ValidateInputParameters(integrationLayerId, assetId, clientId);

            try
            {
                var newWallet = await _apiRunner.RunWithRetriesAsync(() => _api.CreateWalletAsync
                (
                    integrationLayerId,
                    assetId,
                    clientId
                ));

                return newWallet;
            }
            catch (ErrorResponseException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                return null;
            }
        }

        /// <inheritdoc cref="IBlockchainWalletsClient.DeleteWalletAsync" />
        public async Task<bool> DeleteWalletAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            ValidateInputParameters(integrationLayerId, assetId, clientId);

            try
            {
                await _apiRunner.RunWithRetriesAsync(() => _api.DeleteWalletAsync
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

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        /// <inheritdoc cref="IBlockchainWalletsClient.GetAllWalletsAsync" />
        public async Task<IEnumerable<WalletResponse>> GetAllWalletsAsync(Guid clientId, int batchSize = 50)
        {

            if (clientId == Guid.Empty)
            {
                throw new ArgumentException(nameof(clientId));
            }

            string continuationToken = null;
            var wallets = new List<WalletResponse>();

            do
            {
                var response = await GetWalletsAsync(clientId, batchSize, continuationToken);

                continuationToken = response?.ContinuationToken;

                if (response?.Wallets != null &&
                    response.Wallets.Count() != 0)
                {
                    wallets.AddRange(response.Wallets);
                }

            } while (!string.IsNullOrEmpty(continuationToken));

            return wallets;
        }

        /// <inheritdoc cref="IBlockchainWalletsClient.GetAddressAsync" />
        public async Task<AddressResponse> GetAddressAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            var address = await TryGetAddressAsync(integrationLayerId, assetId, clientId);

            if (address == null)
            {
                throw new ResultValidationException("Address not found.");
            }

            return address;
        }

        /// <inheritdoc cref="IBlockchainWalletsClient.GetClientIdAsync" />
        public async Task<Guid> GetClientIdAsync(string integrationLayerId, string assetId, string address)
        {
            var clientId = await TryGetClientIdAsync(integrationLayerId, assetId, address);

            if (!clientId.HasValue)
            {
                throw new ResultValidationException("Client not found.");
            }

            return clientId.Value;
        }

        /// <inheritdoc cref="IBlockchainWalletsClient.TryGetAddressAsync" />
        public async Task<AddressResponse> TryGetAddressAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            ValidateInputParameters(integrationLayerId, assetId, clientId);

            var address = await _apiRunner.RunWithRetriesAsync(() => _api.GetAddressAsync
            (
                integrationLayerId,
                assetId,
                clientId
            ));

            return address;
        }

        /// <inheritdoc cref="IBlockchainWalletsClient.TryGetClientIdAsync" />
        public async Task<Guid?> TryGetClientIdAsync(string integrationLayerId, string assetId, string address)
        {
            ValidateInputParameters(integrationLayerId, assetId, address);

            var clientId = await _apiRunner.RunWithRetriesAsync(() => _api.GetClientIdAsync
            (
                integrationLayerId,
                assetId,
                address
            ));

            return clientId?.ClientId;
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
    }
}
