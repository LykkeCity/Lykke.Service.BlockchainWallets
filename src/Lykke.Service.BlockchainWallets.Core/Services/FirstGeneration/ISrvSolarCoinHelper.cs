using Lykke.Service.BlockchainWallets.Core.FirstGeneration;
using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration
{
    public interface ISrvSolarCoinHelper
    {
        Task<string> SetNewSolarCoinAddress(IWalletCredentials walletCredentials);
    }
}
