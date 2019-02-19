using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Contract;

namespace Lykke.Service.BlockchainWallets.Core.Repositories
{
    public interface IBlockchainWalletsBackupRepository
    {
        Task AddAsync(string integrationLayerId, Guid clientId, string address,
            CreatorType createdBy, bool isPrimary);

        Task<(IEnumerable<(string integrationLayerId, Guid clientId, string address, CreatorType createdBy, bool isPrimary)>
                Entities, string ContinuationToken)>
            GetDataWithContinuationTokenAsync(int take,
                string continuationToken);

        Task DeleteIfExistAsync(string integrationLayerId, Guid clientId, string address);
    }
}
