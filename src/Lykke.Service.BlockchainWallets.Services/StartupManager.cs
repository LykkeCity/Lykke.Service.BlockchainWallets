using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Service.BlockchainWallets.Core.Services;

namespace Lykke.Service.BlockchainWallets.Services
{
    [UsedImplicitly]
    public class StartupManager : IStartupManager
    {
        public StartupManager(ILog log)
        {
            
        }

        public async Task StartAsync()
        {
            await Task.CompletedTask;
        }
    }
}
