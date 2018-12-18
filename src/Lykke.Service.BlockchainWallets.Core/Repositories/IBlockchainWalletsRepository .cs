using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.DTOs;

namespace Lykke.Service.BlockchainWallets.Core.Repositories
{
    public interface IBlockchainWalletsRepository
    {
        Task AddAsync(string blockchainType, Guid clientId, string address,
            CreatorType createdBy, string clientLatestDepositIndexManualPartitionKey = null, bool addAsLatest = true);

        Task DeleteIfExistsAsync(string blockchainType, Guid clientId, string address);

        Task<bool> ExistsAsync(string blockchainType, string address);

        Task<bool> ExistsAsync(string blockchainType, Guid clientId);

        Task<(IEnumerable<WalletDto> Wallets, string ContinuationToken)> GetAllAsync(string blockchainType, 
            Guid clientId, int take, string continuationToken = null);

        Task<(IEnumerable<WalletDto> Wallets, string ContinuationToken)> GetAllAsync(Guid clientId, 
            int take, string continuationToken = null);

        Task<WalletDto> TryGetAsync(string blockchainType, string address);

        Task<WalletDto> TryGetAsync(string blockchainType, Guid clientId);
    }
}
