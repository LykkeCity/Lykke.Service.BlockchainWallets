using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.DTOs.Validation;
using Lykke.Service.BlockchainWallets.Core.Repositories;

namespace Lykke.Service.BlockchainWallets.Tests.Validation
{
    public class BlackListRepositoryFake : IBlackListRepository
    {
        private List<BlackListModel> BlackList { get; set; }

        public BlackListRepositoryFake()
        {
            BlackList = new List<BlackListModel>();
        }

        public Task<BlackListModel> TryGetAsync(string blockchainType, string blockedAddress)
        {
            var model = BlackList.FirstOrDefault(x => x.BlockchainType == blockchainType &&
                                          x.BlockedAddress?.ToLower() == blockedAddress?.ToLower());

            return Task.FromResult(model);
        }

        public Task<(IEnumerable<BlackListModel>, string continuationToken)> TryGetAllAsync(string blockchainType, int take, string continuationToken = null)
        {
            var list = BlackList.Where(x => x.BlockchainType == blockchainType).Take(take);

            return Task.FromResult<(IEnumerable<BlackListModel>, string continuationToken)>((list, null));
        }

        public async Task SaveAsync(BlackListModel model)
        {
            await DeleteAsync(model.BlockchainType, model.BlockedAddress);

            BlackList.Add(model);
        }

        public async Task DeleteAsync(string blockchainType, string blockedAddress)
        {
            var exisitng = await TryGetAsync(blockchainType, blockedAddress);

            if (exisitng != null)
            {
                BlackList.Remove(exisitng);
            }
        }
    }
}
