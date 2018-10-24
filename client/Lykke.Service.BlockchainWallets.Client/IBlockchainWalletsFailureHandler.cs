using System;
using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Client
{
    public interface IBlockchainWalletsFailureHandler
    {
        Task<T> Execute<T>(Func<Task<T>> method, TimeSpan? timeout = null, Func<T> fallbackResult = null);
    }
}
