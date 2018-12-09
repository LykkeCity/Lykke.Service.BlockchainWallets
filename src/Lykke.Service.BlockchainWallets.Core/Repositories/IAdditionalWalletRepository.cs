using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.DTOs;

namespace Lykke.Service.BlockchainWallets.Core.Repositories
{
    public interface IAdditionalWalletRepository
    {
        Task AddAsync(string blockchainType, string assetId, Guid clientId, string address);

        Task DeleteAllAsync(string blockchainType, string assetId, Guid clientId);

        Task<bool> ExistsAsync(string blockchainType, string assetId, Guid clientId);

        Task<WalletDto> TryGetAsync(string blockchainType,  string address);

        Task<(IEnumerable<WalletDto> Wallets, string ContinuationToken)> GetAsync(int take,
            string continuationToken);
    }
}
