using System;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainWallets.Contract.Models;
using Refit;


namespace Lykke.Service.BlockchainWallets.Client
{
    internal interface IBlockchainWalletsApi
    {
        [Post("/api/wallets/{blockchainType}/{assetId}/by-client-ids/{clientId}")]
        Task<WalletResponse> CreateWalletAsync(string blockchainType, string assetId, Guid clientId);

        [Delete("/api/wallets/{blockchainType}/{assetId}/by-client-ids/{clientId}")]
        Task DeleteWalletAsync(string blockchainType, string assetId, Guid clientId);

        [Get("/api/constants/{blockchainType}/address-extension")]
        Task<AddressExtensionConstantsResponse> GetAddressExtensionConstantsAsync(string blockchainType);

        [Get("/api/wallets/{blockchainType}/{assetId}/by-client-ids/{clientId}/address")]
        Task<AddressResponse> GetAddressAsync(string blockchainType, string assetId, Guid clientId);

        [Get("/api/wallets/{blockchainType}/{assetId}/by-addresses/{address}/client-id")]
        Task<ClientIdResponse> GetClientIdAsync(string blockchainType, string assetId, string address);

        [Get("/api/isalive")]
        Task<IsAliveResponse> GetIsAliveAsync();

        [Get("/api/wallets/all/by-client-ids/{clientId}")]
        Task<WalletsResponse> GetWalletsAsync(Guid clientId, int take, string continuationToken);

        [Post("/api/address/merge")]
        Task<MergedAddressResponse> MergeAddressAsync(MergeAddressRequest request);

        [Get("/api/capabilities/{blockchainType}")]
        Task<CapabilititesResponce> GetCapabilititesAsync(string blockchainType);


        [Get("/api/capabilities/{blockchainType}/parse-address/{address}")]
        Task<AddressParseResultResponce> ParseAddressAsync(string blockchainType, string address);
    }
}
