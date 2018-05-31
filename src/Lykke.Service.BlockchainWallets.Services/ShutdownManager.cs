using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Service.BlockchainWallets.Core.Services;

namespace Lykke.Service.BlockchainWallets.Services
{
    [UsedImplicitly]
    public class ShutdownManager : IShutdownManager
    {
        public ShutdownManager(ILog log)
        {
            
        }

        public async Task StopAsync()
        {
            await Task.CompletedTask;
        }
    }
}
