using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.DTOs;

namespace Lykke.Service.BlockchainWallets.Core.Repositories
{
    public interface IBlockchainWalletsRepository
    {
        Task<ChangedPrimaryWalletDto> AddAsync(string blockchainType, Guid clientId, string address,
            CreatorType createdBy, bool addAsLatest = true);

        Task<ChangedPrimaryWalletDto> DeleteIfExistsAsync(string blockchainType, Guid clientId, string address);

        Task<bool> ExistsAsync(string blockchainType, string address);

        Task<bool> ExistsAsync(string blockchainType, Guid clientId);

        Task<(IReadOnlyCollection<WalletDto> Wallets, string ContinuationToken)> GetAllAsync(string blockchainType, 
            Guid clientId, int take, string continuationToken = null);

        Task<(IReadOnlyCollection<WalletDto> Wallets, string ContinuationToken)> GetAllPrimaryAsync(Guid clientId, 
            int take, string continuationToken = null);

        Task<WalletDto> TryGetAsync(string blockchainType, string address);

        Task<WalletDto> TryGetPrimaryAsync(string blockchainType, Guid clientId);

        Task EnsureIndexesCreatedAsync();
    }
}
