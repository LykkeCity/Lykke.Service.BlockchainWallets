using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Contract.Models;

namespace Lykke.Service.BlockchainWallets.Client
{
    /// <summary>
    /// </summary>
    [PublicAPI]
    public interface IBlockchainWalletsClient
    {
        /// <summary>
        ///     BlockchainWallets service host.
        /// </summary>
        string HostUrl { get; }

        /// <summary>
        ///     Creates new wallet and initiates its balance observation.
        /// </summary>
        /// <param name="blockchainType">
        ///     Target blockchain type.
        /// </param>
        /// <param name="assetId">
        ///     Target asset id (as it specified in the integration layer).
        /// </param>
        /// <param name="clientId">
        ///     Lykke client id.
        /// </param>
        /// <param name="walletType">
        ///     Type of wallet.
        /// </param>
        /// <returns>
        ///     Created wallet, or null if operation failed.
        /// </returns>
        Task<WalletResponse> CreateWalletAsync(string blockchainType, string assetId, Guid clientId, WalletType walletType = WalletType.DepositWallet);

        /// <summary>
        ///     Deletes information about wallet from DB and stops its balance observation.
        /// </summary>
        /// <param name="blockchainType">
        ///     Target blockchain type.
        /// </param>
        /// <param name="assetId">
        ///     Target asset id (as it specified in the integration layer).
        /// </param>
        /// <param name="clientId">
        ///     Lykke client id.
        /// </param>
        /// <returns>
        ///     True, if walle has been deleted, false otherwise.
        /// </returns>
        Task<bool> DeleteWalletAsync(string blockchainType, string assetId, Guid clientId);

        /// <summary>
        ///     Returns blockchain address by Lykke client id.
        /// </summary>
        /// <param name="blockchainType">
        ///     Target blockchain type.
        /// </param>
        /// <param name="assetId">
        ///     Target asset id (as it specified in the integration layer).
        /// </param>
        /// <param name="clientId">
        ///     Lykke client id.
        /// </param>
        /// <returns>
        ///     Blockchain address
        /// </returns>
        /// <exception cref="ErrorResponseException">
        ///     Status code: <see cref="HttpStatusCode.NotFound" /> - address is not found.
        /// </exception>
        Task<AddressResponse> GetAddressAsync(string blockchainType, string assetId, Guid clientId);

        /// <summary>
        ///     Returns address extension constants.
        /// </summary>
        /// <param name="blockchainType">
        ///     Target blockchain type.
        /// </param>
        Task<AddressExtensionConstantsResponse> GetAddressExtensionConstantsAsync(string blockchainType);

        /// <summary>
        ///     Returns Lykke clients wallets.
        /// </summary>
        /// <param name="clientId">
        ///     Client Id.
        /// </param>
        /// <param name="batchSize">
        ///     Amount of wallets to retrieve per request to service.
        /// </param>
        /// <returns>
        ///     Lykke client's wallets.
        /// </returns>
        Task<IEnumerable<WalletResponse>> GetAllWalletsAsync(Guid clientId, int batchSize = 50);

        /// <summary>
        ///     Returns Lykke client id by wallet address.
        /// </summary>
        /// <param name="blockchainType">
        ///     Target blockchain type.
        /// </param>
        /// <param name="assetId">
        ///     Target asset id (as it specified in the integration layer).
        /// </param>
        /// <param name="address">
        ///     Wallet public address.
        /// </param>
        /// <returns>
        ///     Lykke client id
        /// </returns>
        /// <exception cref="ErrorResponseException">
        ///     Status code: <see cref="HttpStatusCode.NotFound" /> - client is not found.
        /// </exception>
        Task<Guid> GetClientIdAsync(string blockchainType, string assetId, string address);

        /// <summary>
        ///     Returns service health status.
        /// </summary>
        Task<IsAliveResponse> GetIsAliveAsync();

        /// <summary>
        ///     Returns Lykke clients wallets.
        /// </summary>
        /// <param name="clientId">
        ///     Client Id.
        /// </param>
        /// <param name="take">
        ///     Amount of wallets to retrieve.
        /// </param>
        /// <param name="continuationToken">
        ///     Continuation token for azure storage.
        /// </param>
        /// <returns>
        ///     Lykke client's wallets.
        /// </returns>
        Task<WalletsResponse> GetWalletsAsync(Guid clientId, int take, string continuationToken);

        /// <summary>
        ///    Creates address from base address and address extension ensures it has valid extension.
        /// </summary>
        /// <param name="blockchainType">
        ///    Blockchain type.
        /// </param>
        /// <param name="baseAddress">
        ///    Base address.
        /// </param>
        /// <param name="addressExtension">
        ///    Address extension.
        /// </param>
        /// <returns>
        ///    Merged address.
        /// </returns>
        Task<string> MergeAddressAsync(string blockchainType, string baseAddress, string addressExtension);

        /// <summary>
        ///    Creates address from base address and ensures it has valid extension.
        /// </summary>
        /// <param name="blockchainType">
        ///    Blockchain type.
        /// </param>
        /// <param name="baseAddress">
        ///    Base address.
        /// </param>
        /// <returns>
        ///    Merged address.
        /// </returns>
        Task<string> MergeAddressAsync(string blockchainType, string baseAddress);

        /// <summary>
        ///     Returns blockchain address by Lykke client id.
        /// </summary>
        /// <param name="blockchainType">
        ///     Target blockchain type.
        /// </param>
        /// <param name="assetId">
        ///     Target asset id (as it specified in the integration layer).
        /// </param>
        /// <param name="clientId">
        ///     Lykke client id.
        /// </param>
        /// <returns>
        ///     Blockchain address, if operation succeeded, null otherwise.
        /// </returns>
        Task<AddressResponse> TryGetAddressAsync(string blockchainType, string assetId, Guid clientId);

        /// <summary>
        ///     Returns Lykke client id by wallet address.
        /// </summary>
        /// <param name="blockchainType">
        ///     Target blockchain type.
        /// </param>
        /// <param name="assetId">
        ///     Target asset id (as it specified in the integration layer).
        /// </param>
        /// <param name="address">
        ///     Wallet public address.
        /// </param>
        /// <returns>
        ///     Lykke client id, if operation succeeded, null otherwise.
        /// </returns>
        Task<Guid?> TryGetClientIdAsync(string blockchainType, string assetId, string address);

        /// <summary>
        ///     Returns capabilitites
        /// </summary>
        /// <param name="blockchainType">
        ///     Target blockchain type.
        /// </param>
        Task<CapabilititesResponce> GetCapabilititesAsync(string blockchainType); 

        /// <summary>
        ///     Returns base address and address extension for given address
        /// </summary>
        /// <param name="blockchainType">
        ///     Target blockchain type.
        /// </param>
        /// <param name="address">
        ///     address to parse
        /// </param>
        Task<AddressParseResultResponce> ParseAddressAsync(string blockchainType, string address);

    }
}
