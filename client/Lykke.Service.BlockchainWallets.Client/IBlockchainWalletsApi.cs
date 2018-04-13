using System;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainWallets.Client.Models;
using Refit;

namespace Lykke.Service.BlockchainWallets.Client
{
    internal interface IBlockchainWalletsApi
    {
        [Post("/api/wallets/{blockchainType}/{assetId}/by-client-ids/{clientId}")]
        Task<WalletResponse> CreateWallet(string blockchainType, string assetId, Guid clientId);

        [Delete("/api/wallets/{blockchainType}/{assetId}/by-client-ids/{clientId}")]
        Task DeleteWallet(string blockchainType, string assetId, Guid clientId);

        [Get("/api/wallets/{blockchainType}/{assetId}/by-client-ids/{clientId}/address")]
        Task<AddressResponse> GetAddress(string blockchainType, string assetId, Guid clientId);

        [Get("/api/wallets/{blockchainType}/{assetId}/by-addresses/{address}/client-id")]
        Task<ClientIdResponse> GetClientId(string blockchainType, string assetId, string address);

        [Get("/api/isalive")]
        Task<IsAliveResponse> GetIsAliveAsync();

        [Get("/api/wallets/all/by-client-ids/{clientId}")]
        Task<WalletsResponse> GetClientWalletsAsync(Guid clientId, int take, string continuationToken);
    }
}
