using System;
using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IWalletService
    {
        Task ConvertDefaultToAdditionalAsync(string integrationLayerId, string assetId);

        Task<string> CreateWalletAsync(string integrationLayerId, string assetId, Guid clientId);

        Task<bool> DefaultWalletExistsAsync(string integrationLayerId, string assetId, Guid clientId);

        Task DeleteWalletsAsync(string integrationLayerId, string assetId, Guid clientId);

        Task<string> GetDefaultAddressAsync(string integrationLayerId, string assetId, Guid clientId);

        Task<Guid?> GetClientIdAsync(string integrationLayerId, string assetId, string address);
        
        Task<bool> WalletExistsAsync(string integrationLayerId, string assetId, Guid clientId);
    }
}
