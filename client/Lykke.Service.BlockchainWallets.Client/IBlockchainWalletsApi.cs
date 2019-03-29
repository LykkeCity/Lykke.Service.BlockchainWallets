using System;
using System.Data;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Contract.Models;
using Lykke.Service.BlockchainWallets.Contract.Models.BlackLists;
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

        #region Validation_And_Black_Lists

        /// <summary>
        /// This method is used to update black list record.
        /// </summary>
        /// <param name="updateModel"></param>
        /// <returns></returns>
        [Put("/api/blockchains/{blockchainType}/black-addresses/{address}")]
        Task UpdateBlackListAsync([FromRoute]string blockchainType, [FromRoute]string address, [FromQuery]bool isCaseSensitive);

        /// <summary>
        /// This method is used to add address into the black list.
        /// </summary>
        /// <param name="createModel"></param>
        /// <returns></returns>
        [Post("/api/blockchains/{blockchainType}/black-addresses/{address}")]
        Task CreateBlackListAsync([FromRoute]string blockchainType, [FromRoute]string address, [FromQuery]bool isCaseSensitive);

        /// <summary>
        /// Delete record by address.
        /// </summary>
        /// <param name="blockchainType"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        [Delete("/api/blockchains/{blockchainType}/black-addresses/{address}")]
        Task DeleteBlackListAsync(string blockchainType, string address);

        /// <summary>
        /// Get black list record if exists
        /// </summary>
        /// <param name="blockchainType"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        [Get("/api/blockchains/{blockchainType}/black-addresses/{address}")]
        Task<BlackListResponse> GetBlackListAsync(string blockchainType, string address);

        /// <summary>
        /// Enumerate through black address of specified blockchain type.
        /// </summary>
        /// <param name="blockchainType"></param>
        /// <param name="take"></param>
        /// <param name="continuationToken"></param>
        /// <returns></returns>
        [Get("/api/blockchains/{blockchainType}/black-addresses")]
        Task<BlackListEnumerationResponse> GetBlackListsAsync(string blockchainType,
            [FromQuery] int take,
            [FromQuery] string continuationToken);

        /// <summary>
        /// Retrieves info whether address is in black list or not
        /// </summary>
        /// <param name="blockchainType"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        [Get("/api/blockchains/{blockchainType}/black-addresses/{address}/is-blocked")]
        Task<IsBlockedResponse> IsBlockedAsync(string blockchainType, string address);

        /// <summary>
        /// Validate cashout destination address
        /// </summary>
        /// <param name="blockchainType"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        [Get("/api/blockchains/{blockchainType}/cashout-destinations/{address}/allowability")]
        Task<CashoutValidityResult> CashoutCheckAsync(string blockchainType, string address);

        #endregion
    }
}
