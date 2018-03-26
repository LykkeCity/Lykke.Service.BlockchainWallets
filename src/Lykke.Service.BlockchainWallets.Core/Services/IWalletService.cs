using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.Domain.Wallet;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IWalletService
    {
        Task<string> CreateWalletAsync(string integrationLayerId, string assetId, Guid clientId);

        Task<bool> DefaultWalletExistsAsync(string integrationLayerId, string assetId, Guid clientId);

        Task DeleteWalletsAsync(string integrationLayerId, string assetId, Guid clientId);

        Task<string> GetDefaultAddressAsync(string integrationLayerId, string assetId, Guid clientId);

        Task<Guid?> GetClientIdAsync(string integrationLayerId, string assetId, string address);
        
        Task<bool> WalletExistsAsync(string integrationLayerId, string assetId, Guid clientId);

        Task<(IEnumerable<IWallet>, string continuationToken)> GetClientWalletsAsync(Guid clientId, int take,
            string continuationToken);
    }
}
