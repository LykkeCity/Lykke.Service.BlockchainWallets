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
using Common;
using Lykke.Service.BlockchainWallets.Client.ClientGenerator;
using Lykke.Service.BlockchainWallets.Client.DelegatingMessageHandlers;


namespace Lykke.Service.BlockchainWallets.Client
{
    [PublicAPI]
    public class BlockchainWalletsClient : IBlockchainWalletsClient, IDisposable
    {
        private readonly IBlockchainWalletsApi _api;
        private readonly ApiRunner _apiRunner;
        private readonly ILog _log;

        [Obsolete("Please, use the overload which consumes ILogFactory instead.")]
        public BlockchainWalletsClient(string hostUrl, ILog log, IBlockchainWalletsApiFactory blockchainWalletsApiFactory,
            int retriesCount = 5)
        {
            HostUrl = hostUrl ?? throw new ArgumentNullException(nameof(hostUrl));
            _log = log ?? throw new ArgumentNullException(nameof(log));
             
            _api = blockchainWalletsApiFactory.CreateNew(hostUrl, false, null, new HttpErrorLoggingHandler(_log));
            _apiRunner = new ApiRunner(retriesCount);
        }

        //ctor without blockchainWalletsApiFactory
        public BlockchainWalletsClient(string hostUrl,
            ILogFactory logFactory,
            int retriesCount = 5,
            params DelegatingHandler[] handlers) : this(hostUrl, 
            logFactory, 
            new BlockchainWalletsApiFactory(), 
            retriesCount,
            handlers)
        {
        }

        public BlockchainWalletsClient(string hostUrl, 
            ILogFactory logFactory,
            IBlockchainWalletsApiFactory blockchainWalletsApiFactory, 
            int retriesCount = 5, 
            params DelegatingHandler[] handlers)
        {
            HostUrl = hostUrl ?? throw new ArgumentNullException(nameof(hostUrl));
            if (logFactory == null)
                throw new ArgumentNullException(nameof(logFactory));
            _log = logFactory.CreateLog(this);

            List<DelegatingHandler> handlersList = new List<DelegatingHandler>(handlers?.Length ?? 0 + 1);
            handlersList.Add(new HttpErrorLoggingHandler(logFactory));
            if (handlers?.Any() ?? false)
            {
                handlersList.AddRange(handlers);
            }

            _api = blockchainWalletsApiFactory.CreateNew(hostUrl, false, null, handlersList.ToArray());
            _apiRunner = new ApiRunner(retriesCount);
        }

        /// <inheritdoc cref="IBlockchainWalletsClient.HostUrl" />
        public string HostUrl { get; }


        /// <inheritdoc cref="IBlockchainWalletsClient.GetIsAliveAsync" />
        public async Task<IsAliveResponse> GetIsAliveAsync()
        {
            return await ApiRunner.RunAsync(() => _api.GetIsAliveAsync());
        }

        /// <inheritdoc cref="IBlockchainWalletsClient.GetWalletsAsync" />
        [Obsolete]
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
        [Obsolete]
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
                throw new DuplicationWalletException($"Deposit wallet already exists. Context: {new {blockchainType, assetId, clientId, walletType}.ToJson()}");
            }
        }

        /// <inheritdoc cref="IBlockchainWalletsClient.DeleteWalletAsync" />
        [Obsolete]
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
                _log.WriteWarning(nameof(TryGetAddressExtensionConstantsAsync), blockchainType, "Unable to obtain address extension constants for blockchain type", e);
                return null;
            }
        }

        /// <inheritdoc cref="IBlockchainWalletsClient.GetAllWalletsAsync" />
        [Obsolete]
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
        [Obsolete]
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
        public async Task<Guid> GetClientIdAsync(string blockchainType, string address)
        {
            var clientId = await TryGetClientIdAsync(blockchainType, address);

            if (!clientId.HasValue)
            {
                throw new ResultValidationException("Client not found.");
            }

            return clientId.Value;
        }


        /// <inheritdoc cref="IBlockchainWalletsClient.TryGetAddressAsync" />
        [Obsolete]
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
        public async Task<Guid?> TryGetClientIdAsync(string blockchainType, string address)
        {
            ValidateBlockchainTypeAndAddress(blockchainType, address);

            var clientId = await _apiRunner.RunWithRetriesAsync(() => _api.GetClientIdAsync
            (
                blockchainType,                
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

        #region Multiple Client Deposits Methods

        public async Task<BlockchainWalletResponse> CreateWalletAsync(string blockchainType, Guid clientId, CreatorType createdBy)
        {
            if (clientId == Guid.Empty)
            {
                throw new ArgumentException(nameof(clientId));
            }

            var response = await _apiRunner.RunWithRetriesAsync(() => _api.CreateWalletAsync(blockchainType, clientId, createdBy));

            return response;
        }

        public async Task<bool> DeleteWalletAsync(string blockchainType, Guid clientId, string address)
        {
            if (clientId == Guid.Empty)
            {
                throw new ArgumentException(nameof(clientId));
            }

            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException(nameof(address));
            }

            await _apiRunner.RunWithRetriesAsync(() => _api.DeleteWalletAsync(blockchainType, clientId, address));

            return true;
        }

        public async Task<BlockchainWalletsResponse> GetWalletsAsync(string blockchainType, Guid clientId, int take, string continuationToken)
        {
            if (clientId == Guid.Empty)
            {
                throw new ArgumentException(nameof(clientId));
            }

            var response = await _apiRunner.RunWithRetriesAsync(() => _api.GetWalletsAsync(blockchainType, clientId, take, continuationToken));

            return response;
        }

        public async Task<BlockchainWalletResponse> GetWalletAsync(string blockchainType, string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException(nameof(address));
            }

            var response = await _apiRunner.RunWithRetriesAsync(() => _api.GetWalletAsync(blockchainType, address));

            return response;
        }

        public async Task<CreatedByResponse> GetWalletsCreatorAsync(string blockchainType, string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException(nameof(address));
            }

            var response = await _apiRunner.RunWithRetriesAsync(() => _api.GetCreatedByAsync(blockchainType, address));

            return response;
        }

        public async Task<BlockchainWalletsResponse> GetClientWalletsAsync(Guid clientId, int take, string continuationToken)
        {
            if (clientId == Guid.Empty)
            {
                throw new ArgumentException(nameof(clientId));
            }

            var response = await _apiRunner.RunWithRetriesAsync(() => _api.GetClientWalletsAsync(clientId, take, continuationToken));

            return response;
        }

        #endregion


        #region Private Methods

        private static void ValidateInputParameters(string blockchainType)
        {
            if (string.IsNullOrEmpty(blockchainType))
            {
                throw new ArgumentException(nameof(blockchainType));
            }
        }

        private static void ValidateBlockchainTypeAndAddress(string blockchainType, string address)
        {
            ValidateInputParameters(blockchainType);

            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException(nameof(address));
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

        #endregion
    }
}
