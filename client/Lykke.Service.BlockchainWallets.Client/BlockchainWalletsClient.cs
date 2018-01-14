using System;
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
        private readonly ApiRunner             _apiRunner;
        private readonly HttpClient            _httpClient;
        private readonly ILog                  _log;


        public BlockchainWalletsClient(string hostUrl, ILog log, int retriesCount = 5)
        {
            _log    = log;
            HostUrl = hostUrl ?? throw new ArgumentNullException(nameof(hostUrl));

            _httpClient = new HttpClient
            {
                BaseAddress           = new Uri(hostUrl),
                DefaultRequestHeaders =
                {
                    {
                        "User-Agent",
                        $"{PlatformServices.Default.Application.ApplicationName}/{PlatformServices.Default.Application.ApplicationVersion}"
                    }
                }
            };

            _api       = RestService.For<IBlockchainWalletsApi>(_httpClient);
            _apiRunner = new ApiRunner(retriesCount);
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
            if (string.IsNullOrEmpty(integrationLayerId))
            {
                throw new ArgumentException(nameof(integrationLayerId));
            }

            if (string.IsNullOrEmpty(assetId))
            {
                throw new ArgumentException(nameof(assetId));
            }

            try
            {
                var newWallet = await _apiRunner.RunWithRetriesAsync(() => _api.CreateWallet
                (
                    integrationLayerId,
                    assetId,
                    new CreateWalletRequest
                    {
                        ClientId = clientId
                    })
                );

                return newWallet.Address;
            }
            catch (ErrorResponseException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (ErrorResponseException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                return null;
            }
            catch (Exception e)
            {
                await LogErrorAsync(e, nameof(GetClientIdAsync));

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteWalletAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            if (string.IsNullOrEmpty(integrationLayerId))
            {
                throw new ArgumentException(nameof(integrationLayerId));
            }

            if (string.IsNullOrEmpty(assetId))
            {
                throw new ArgumentException(nameof(assetId));
            }

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
            catch (ErrorResponseException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception e)
            {
                await LogErrorAsync(e, nameof(GetClientIdAsync));

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Guid> GetClientIdAsync(string integrationLayerId, string assetId, string address)
        {
            if (string.IsNullOrEmpty(integrationLayerId))
            {
                throw new ArgumentException(nameof(integrationLayerId));
            }

            if (string.IsNullOrEmpty(assetId))
            {
                throw new ArgumentException(nameof(assetId));
            }

            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException(nameof(address));
            }

            var clientId = await _apiRunner.RunWithRetriesAsync(() => _api.GetClientId
            (
                integrationLayerId,
                assetId,
                address
            ));

            return clientId.ClientId;
        }

        /// <inheritdoc />
        public async Task<Guid?> TryGetClientIdAsync(string integrationLayerId, string assetId, string address)
        {
            try
            {
                return await GetClientIdAsync(integrationLayerId, assetId, address);
            }
            catch (ErrorResponseException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception e)
            {
                await LogErrorAsync(e, nameof(GetClientIdAsync));

                throw;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
        

        private async Task LogErrorAsync(Exception e, string process)
        {
            await _log.WriteErrorAsync
            (
                component: nameof(BlockchainWalletsClient),
                process:   process,
                context:   string.Empty,
                exception: e,
                dateTime:  DateTime.UtcNow
            );
        }
    }
}
