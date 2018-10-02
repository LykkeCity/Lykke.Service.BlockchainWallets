using System;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Contract.Models;
using Microsoft.AspNetCore.Mvc;
using Refit;


namespace Lykke.Service.BlockchainWallets.Client
{
    public interface IBlockchainWalletsApi
    {
        [Obsolete]
        [Post("/api/wallets/{blockchainType}/{assetId}/by-client-ids/{clientId}")]
        Task<WalletResponse> CreateWalletAsync(string blockchainType, string assetId, Guid clientId);

        [Obsolete]
        [Delete("/api/wallets/{blockchainType}/{assetId}/by-client-ids/{clientId}")]
        Task DeleteWalletAsync(string blockchainType, string assetId, Guid clientId);

        [Get("/api/constants/{blockchainType}/address-extension")]
        Task<AddressExtensionConstantsResponse> GetAddressExtensionConstantsAsync(string blockchainType);

        [Obsolete]
        [Get("/api/wallets/{blockchainType}/{assetId}/by-client-ids/{clientId}/address")]
        Task<AddressResponse> GetAddressAsync(string blockchainType, string assetId, Guid clientId);

        [Obsolete]
        [Get("/api/blockchains/{blockchainType}/wallets/{address}/client-id")]
        Task<ClientIdResponse> GetClientIdAsync(string blockchainType, string address);

        [Get("/api/isalive")]
        Task<IsAliveResponse> GetIsAliveAsync();

        [Obsolete]
        [Get("/api/wallets/all/by-client-ids/{clientId}")]
        Task<WalletsResponse> GetWalletsAsync(Guid clientId, int take, string continuationToken);

        [Get("/api/{blockchainType}/address/merged/{baseAddress}/{addressExtension}")]
        Task<MergedAddressResponse> MergeAddressAsync(string blockchainType, string baseAddress, string addressExtension);

        [Get("/api/{blockchainType}/address/merged/{baseAddress}")]
        Task<MergedAddressResponse> MergeAddressAsync(string blockchainType, string baseAddress);

        [Get("/api/capabilities/{blockchainType}")]
        Task<CapabilititesResponce> GetCapabilititesAsync(string blockchainType);

        [Get("/api/{blockchainType}/address/parsed/{address}")]
        Task<AddressParseResultResponce> ParseAddressAsync(string blockchainType, string address);

        #region Multiple Deposits for Client

        [Post("/api/blockchains/{blockchainType}/clients/{clientId}/wallets")]
        Task<BlockchainWalletResponse> CreateWalletAsync(string blockchainType, Guid clientId, [FromQuery] CreatorType createdBy);

        [Get("/api/blockchains/{blockchainType}/clients/{clientId}/wallets")]
        Task<BlockchainWalletsResponse> GetWalletsAsync(string blockchainType, Guid clientId, [FromQuery] int take, 
            [FromQuery] string continuationToken);

        [Delete("/api/blockchains/{blockchainType}clients/{clientId}/wallets/{address}")]
        Task DeleteWalletAsync(string blockchainType, Guid clientId, string address);

        [Get("/api/blockchains/{blockchainType}/wallets/{address}")]
        Task<BlockchainWalletResponse> GetWalletAsync(string blockchainType, string address);

        [Get("/api/blockchains/{blockchainType}/wallets/{address}/created-by")]
        Task<CreatedByResponse> GetCreatedByAsync(string blockchainType, string address);

        [Get("/api/clients/{clientId}/actual-wallets")]
        Task<BlockchainWalletsResponse> GetClientWalletsAsync(Guid clientId, [FromQuery] int take,
            [FromQuery] string continuationToken);

        #endregion

    }
}
