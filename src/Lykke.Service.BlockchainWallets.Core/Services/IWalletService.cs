using System;
using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IWalletService
    {
        Task<string> CreateWalletAsync(string integrationLayerId, string assetId, Guid clientId);

        Task DeleteWalletAsync(string integrationLayerId, string assetId, Guid clientId);

        Task<string> GetAddressAsync(string integrationLayerId, string assetId, Guid clientId);

        Task<Guid?> GetClientIdAsync(string integrationLayerId, string assetId, string address);

        Task<bool> WalletExistsAsync(string integrationLayerId, string assetId, Guid clientId);
    }
}
