using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Contract;

namespace Lykke.Service.BlockchainWallets.Core.Repositories
{
    public interface IBlockchainWalletsBackupRepository
    {
        Task AddAsync(string blockchainType, Guid clientId, string address,
            CreatorType createdBy);

        Task<(IReadOnlyCollection<(string blockchainType, Guid clientId, string address, CreatorType createdBy, bool isPrimary)>
                Entities, string ContinuationToken)>
            GetDataWithContinuationTokenAsync(int take,
                string continuationToken);

        Task SetPrimaryWalletAsync(string blockchainType, Guid clientId, string address, int version);
        Task DeleteIfExistAsync(string blockchainType, Guid clientId, string address);
    }
}
