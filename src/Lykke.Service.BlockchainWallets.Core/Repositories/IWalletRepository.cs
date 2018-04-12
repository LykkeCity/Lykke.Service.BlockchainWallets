using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.DTOs;

namespace Lykke.Service.BlockchainWallets.Core.Repositories
{
    public interface IWalletRepository
    {
        Task AddAsync(string integrationLayerId, string assetId, Guid clientId, string address);

        Task DeleteIfExistsAsync(string integrationLayerId, string assetId, Guid clientId);

        Task<bool> ExistsAsync(string integrationLayerId, string assetId, string address);

        Task<bool> ExistsAsync(string integrationLayerId, string assetId, Guid clientId);

        Task<(IEnumerable<WalletDto> Wallets, string ContinuationToken)> GetAllAsync(Guid clientId, int take, string continuationToken = null);

        Task<WalletDto> TryGetAsync(string integrationLayerId, string assetId, string address);

        Task<WalletDto> TryGetAsync(string integrationLayerId, string assetId, Guid clientId);
    }
}
