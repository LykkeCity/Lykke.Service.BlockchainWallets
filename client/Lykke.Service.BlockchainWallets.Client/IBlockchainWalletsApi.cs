using System;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainWallets.Client.Models;
using Refit;

namespace Lykke.Service.BlockchainWallets.Client
{
    internal interface IBlockchainWalletsApi
    {
        [Get("/api/isalive")]
        Task<IsAliveResponse> GetIsAliveAsync();

        [Post("/api/wallets/{integrationLayerId}/{assetId}/by-client-ids/{clientId}")]
        Task<WalletResponse> CreateWallet(string integrationLayerId, string assetId, Guid clientId);

        [Delete("/api/wallets/{integrationLayerId}/{assetId}/by-client-ids/{clientId}")]
        Task DeleteWallet(string integrationLayerId, string assetId, Guid clientId);

        [Get("/api/wallets/{integrationLayerId}/{assetId}/by-client-ids/{clientId}/address")]
        Task<AddressResponse> GetAddress(string integrationLayerId, string assetId, Guid clientId);

        [Get("/api/wallets/{integrationLayerId}/{assetId}/by-addresses/{address}/client-id")]
        Task<ClientIdResponse> GetClientId(string integrationLayerId, string assetId, string address);
    }
}
