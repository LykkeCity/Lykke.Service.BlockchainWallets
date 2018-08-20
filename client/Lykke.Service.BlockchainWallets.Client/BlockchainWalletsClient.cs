using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Contract.Models;
using Microsoft.Extensions.PlatformAbstractions;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Lykke.Common.Log;


namespace Lykke.Service.BlockchainWallets.Client
{
    [PublicAPI]
    public class BlockchainWalletsClient : IBlockchainWalletsClient, IDisposable
    {
        private readonly IBlockchainWalletsApi _api;
        private readonly ApiRunner _apiRunner;
        private readonly HttpClient _httpClient;
        private readonly ILog _log;

        [Obsolete("Please, use the overload which consumes ILogFactory instead.")]
        public BlockchainWalletsClient(string hostUrl, ILog log, int retriesCount = 5)
        {
            HostUrl = hostUrl ?? throw new ArgumentNullException(nameof(hostUrl));
            _log = log ?? throw new ArgumentNullException(nameof(log));

            _httpClient = new HttpClient(new HttpErrorLoggingHandler(_log))
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

        public BlockchainWalletsClient(string hostUrl, ILogFactory logFactory, int retriesCount = 5)
        {
            HostUrl = hostUrl ?? throw new ArgumentNullException(nameof(hostUrl));

            _httpClient = new HttpClient(new HttpErrorLoggingHandler(logFactory))
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
            return await ApiRunner.RunAsync(() => _api.GetIsAliveAsync());
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

        /// <inheritdoc cref="IBlockchainWalletsClient.MergeAddressAsync" />
        public async Task<string> MergeAddressAsync(string blockchainType, string baseAddress, string addressExtension)
        {
            ValidateInputParameters(blockchainType);

            if (string.IsNullOrEmpty(baseAddress))
            {
                throw new ArgumentException(nameof(baseAddress));
            }

            Func<Task<MergedAddressResponse>> @delegate = !string.IsNullOrEmpty(addressExtension) ? 
                (Func<Task<MergedAddressResponse>>)(() => _api.MergeAddressAsync(blockchainType, baseAddress, addressExtension)) :
                (Func<Task<MergedAddressResponse>>)(() => _api.MergeAddressAsync(blockchainType, baseAddress));

            var response = await _apiRunner.RunWithRetriesAsync(() => @delegate());

            return response.Address;
        }

        /// <inheritdoc cref="IBlockchainWalletsClient.CreateWalletAsync" />
        public async Task<WalletResponse> CreateWalletAsync(string blockchainType, string assetId, Guid clientId, WalletType walletType = WalletType.DepositWallet)
        {
            ValidateInputParameters(blockchainType, assetId, clientId);

            try
            {
                var newWallet = await _apiRunner.RunWithRetriesAsync(() => _api.CreateWalletAsync
                (
                    blockchainType,
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
        public async Task<bool> DeleteWalletAsync(string blockchainType, string assetId, Guid clientId)
        {
            ValidateInputParameters(blockchainType, assetId, clientId);

            try
            {
                await _apiRunner.RunWithRetriesAsync(() => _api.DeleteWalletAsync
                (
                    blockchainType,
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

        /// <inheritdoc cref="IBlockchainWalletsClient.GetAddressExtensionConstantsAsync" />
        public async Task<AddressExtensionConstantsResponse> GetAddressExtensionConstantsAsync(string blockchainType)
        {
            ValidateInputParameters(blockchainType);

            var constants = await _apiRunner.RunWithRetriesAsync(() => _api.GetAddressExtensionConstantsAsync
            (
                blockchainType
            ));

            return constants;
        }

        /// <inheritdoc cref="IBlockchainWalletsClient.TryGetAddressExtensionConstantsAsync" />
        public async Task<AddressExtensionConstantsResponse> TryGetAddressExtensionConstantsAsync(string blockchainType)
        {
            try
            {
                var result = await GetAddressExtensionConstantsAsync(blockchainType);
                return result;
            }
            catch (Exception e)
            {
                _log.Warning("Unable to obtain address extension constants for blockchain type", e, blockchainType);
                return null;
            }
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
        public async Task<AddressResponse> GetAddressAsync(string blockchainType, string assetId, Guid clientId)
        {
            var address = await TryGetAddressAsync(blockchainType, assetId, clientId);

            if (address == null)
            {
                throw new ResultValidationException("Address not found.");
            }

            return address;
        }

        /// <inheritdoc cref="IBlockchainWalletsClient.GetClientIdAsync" />
        public async Task<Guid> GetClientIdAsync(string blockchainType, string assetId, string address)
        {
            var clientId = await TryGetClientIdAsync(blockchainType, assetId, address);

            if (!clientId.HasValue)
            {
                throw new ResultValidationException("Client not found.");
            }

            return clientId.Value;
        }

        /// <inheritdoc cref="IBlockchainWalletsClient.TryGetAddressAsync" />
        public async Task<AddressResponse> TryGetAddressAsync(string blockchainType, string assetId, Guid clientId)
        {
            ValidateInputParameters(blockchainType, assetId, clientId);

            var address = await _apiRunner.RunWithRetriesAsync(() => _api.GetAddressAsync
            (
                blockchainType,
                assetId,
                clientId
            ));

            return address;
        }

        /// <inheritdoc cref="IBlockchainWalletsClient.TryGetClientIdAsync" />
        public async Task<Guid?> TryGetClientIdAsync(string blockchainType, string assetId, string address)
        {
            ValidateInputParameters(blockchainType, assetId, address);

            var clientId = await _apiRunner.RunWithRetriesAsync(() => _api.GetClientIdAsync
            (
                blockchainType,
                assetId,
                address
            ));

            return clientId?.ClientId;
        }

        public async Task<CapabilititesResponce> GetCapabilititesAsync(string blockchainType)
        {
            ValidateInputParameters(blockchainType);

            var capabilitites = await _apiRunner.RunWithRetriesAsync(() => _api.GetCapabilititesAsync
            (
                blockchainType
            ));

            return capabilitites;
        }

        public async Task<AddressParseResultResponce> ParseAddressAsync(string blockchainType, string address)
        {
            ValidateInputParameters(blockchainType);

            var parseResult = await _apiRunner.RunWithRetriesAsync(() => _api.ParseAddressAsync
            (
                blockchainType,
                address
            ));

            return parseResult;
        }

        private static void ValidateInputParameters(string blockchainType)
        {
            if (string.IsNullOrEmpty(blockchainType))
            {
                throw new ArgumentException(nameof(blockchainType));
            }
        }

        private static void ValidateInputParameters(string blockchainType, string assetId)
        {
            ValidateInputParameters(blockchainType);
            
            if (string.IsNullOrEmpty(assetId))
            {
                throw new ArgumentException(nameof(assetId));
            }
        }

        private static void ValidateInputParameters(string blockchainType, string assetId, string address)
        {
            ValidateInputParameters(blockchainType, assetId);

            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException(nameof(address));
            }
        }

        private static void ValidateInputParameters(string blockchainType, string assetId, Guid clientId)
        {
            ValidateInputParameters(blockchainType, assetId);

            if (clientId == Guid.Empty)
            {
                throw new ArgumentException(nameof(clientId));
            }
        }
    }
}
