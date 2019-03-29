using Lykke.Service.BlockchainWallets.Core.DTOs.Validation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Core.Repositories
{
    public interface IBlackListRepository
    {
        Task<BlackListModel> TryGetAsync(string blockchainType, string blockedAddress);

        Task<(IEnumerable<BlackListModel>, string continuationToken)> TryGetAllAsync(string blockchainType, int take,
            string continuationToken = null);

        Task SaveAsync(BlackListModel model);

        Task DeleteAsync(string blockchainType, string blockedAddress);
    }
}
