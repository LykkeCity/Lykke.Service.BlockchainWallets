using System;
using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Core.Domain.Wallet
{
    public interface IWalletRepository
    {
        Task AddAsync(string integrationLayerId, string assetId, Guid clientId, string address);

        Task DeleteIfExistsAsync(string integrationLayerId, string assetId, Guid clientId);

        Task<bool> ExistsAsync(string integrationLayerId, string assetId, string address);

        Task<bool> ExistsAsync(string integrationLayerId, string assetId, Guid clientId);
        
        Task<IWallet> TryGetAsync(string integrationLayerId, string assetId, string address);

        Task<IWallet> TryGetAsync(string integrationLayerId, string assetId, Guid clientId);
    }
}
