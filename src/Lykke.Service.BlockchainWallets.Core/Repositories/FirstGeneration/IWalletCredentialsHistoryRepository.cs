using Lykke.Service.BlockchainWallets.Core.FirstGeneration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Core.Repositories.FirstGeneration
{
    public interface IWalletCredentialsHistoryRepository
    {
        Task InsertHistoryRecord(IWalletCredentials oldWalletCredentials);
        Task<IEnumerable<string>> GetPrevMultisigsForUser(string clientId);
    }
}
