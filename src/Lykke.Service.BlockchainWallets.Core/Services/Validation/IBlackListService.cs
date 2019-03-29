using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.DTOs.Validation;

namespace Lykke.Service.BlockchainWallets.Core.Services.Validation
{
    public interface IBlackListService
    {
        Task<bool> IsBlockedAsync(string blockchainType, string blockedAddress);

        Task<bool> IsBlockedWithoutAddressValidationAsync(string blockchainType, string blockedAddress);

        Task<BlackListModel> TryGetAsync(string blockchainType, string blockedAddress);

        Task<(IEnumerable<BlackListModel>, string continuationToken)> TryGetAllAsync(string blockchainType, int take,
            string continuationToken = null);

        Task SaveAsync(BlackListModel model);

        Task DeleteAsync(string blockchainType, string blockedAddress);
    }
}
