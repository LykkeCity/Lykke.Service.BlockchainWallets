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

        [Post("/api/wallets/{integrationLayerId}/{assetId}")]
        Task<WalletResponse> CreateWallet(string integrationLayerId, string assetId, [Body] CreateWalletRequest body);

        [Delete("/api/wallets/{integrationLayerId}/{assetId}/by-client-ids/{clientId}")]
        Task DeleteWallet(string integrationLayerId, string assetId, Guid clientId);

        [Get("/api/wallets/{integrationLayerId}/{assetId}/by-addresses/{address}/clientId")]
        Task<ClientIdResponse> GetClientId(string integrationLayerId, string assetId, string address);
    }
}
