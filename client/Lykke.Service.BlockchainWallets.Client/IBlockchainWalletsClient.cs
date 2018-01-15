using System;
using System.Net;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;

namespace Lykke.Service.BlockchainWallets.Client
{
    /// <summary>
    /// 
    /// </summary>
    public interface IBlockchainWalletsClient
    {
        /// <summary>
        ///    BlockchainWallets service host.
        /// </summary>
        string HostUrl { get; }

        /// <summary>
        ///    Returns service health status.
        /// </summary>
        Task<IsAliveResponse> GetIsAliveAsync();

        /// <summary>
        ///    Creates new wallet and initiates its balance observation.
        /// </summary>
        /// <param name="integrationLayerId">
        ///    Target blockchain integration id.
        /// </param>
        /// <param name="assetId">
        ///    Target asset id (as it specified in the integration layer).
        /// </param>
        /// <param name="clientId">
        ///    Lykke client id.
        /// </param>
        /// <returns>
        ///    Address of created wallet, or null if operation failed.
        /// </returns>
        Task<string> CreateWalletAsync(string integrationLayerId, string assetId, Guid clientId);

        /// <summary>
        ///    Deletes information about wallet from DB and stops its balance observation.
        /// </summary>
        /// <param name="integrationLayerId">
        ///    Target blockchain integration id.
        /// </param>
        /// <param name="assetId">
        ///    Target asset id (as it specified in the integration layer).
        /// </param>
        /// <param name="clientId">
        ///    Lykke client id.
        /// </param>
        /// <returns>
        ///    True, if walle has been deleted, false otherwise.
        /// </returns>
        Task<bool> DeleteWalletAsync(string integrationLayerId, string assetId, Guid clientId);

        /// <summary>
        ///    Returns blockchain address by Lykke client id.
        /// </summary>
        /// <param name="integrationLayerId">
        ///    Target blockchain integration id.
        /// </param>
        /// <param name="assetId">
        ///    Target asset id (as it specified in the integration layer).
        /// </param>
        /// <param name="clientId">
        ///    Lykke client id.
        /// </param>
        /// <returns>
        ///    Blockchain address
        /// </returns>
        /// <exception cref="ErrorResponseException">
        ///    Status code: <see cref="HttpStatusCode.NotFound"/> - address is not found.
        /// </exception>
        Task<string> GetAddressAsync(string integrationLayerId, string assetId, Guid clientId);

        /// <summary>
        ///    Returns Lykke client id by wallet address.
        /// </summary>
        /// <param name="integrationLayerId">
        ///    Target blockchain integration id.
        /// </param>
        /// <param name="assetId">
        ///    Target asset id (as it specified in the integration layer).
        /// </param>
        /// <param name="address">
        ///    Wallet public address.
        /// </param>
        /// <returns>
        ///    Lykke client id
        /// </returns>
        /// <exception cref="ErrorResponseException">
        ///    Status code: <see cref="HttpStatusCode.NotFound"/> - client is not found.
        /// </exception>
        Task<Guid> GetClientIdAsync(string integrationLayerId, string assetId, string address);

        /// <summary>
        ///    Returns blockchain address by Lykke client id.
        /// </summary>
        /// <param name="integrationLayerId">
        ///    Target blockchain integration id.
        /// </param>
        /// <param name="assetId">
        ///    Target asset id (as it specified in the integration layer).
        /// </param>
        /// <param name="clientId">
        ///    Lykke client id.
        /// </param>
        /// <returns>
        ///    Blockchain address, if operation succeeded, null otherwise.
        /// </returns>
        Task<string> TryGetAddressAsync(string integrationLayerId, string assetId, Guid clientId);

        /// <summary>
        ///    Returns Lykke client id by wallet address.
        /// </summary>
        /// <param name="integrationLayerId">
        ///    Target blockchain integration id.
        /// </param>
        /// <param name="assetId">
        ///    Target asset id (as it specified in the integration layer).
        /// </param>
        /// <param name="address">
        ///    Wallet public address.
        /// </param>
        /// <returns>
        ///    Lykke client id, if operation succeeded, null otherwise.
        /// </returns>
        Task<Guid?> TryGetClientIdAsync(string integrationLayerId, string assetId, string address);
    }
}
