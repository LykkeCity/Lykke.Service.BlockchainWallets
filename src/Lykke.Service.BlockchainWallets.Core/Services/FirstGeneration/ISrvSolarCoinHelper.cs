using System;
using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration
{
    public interface ISrvSolarCoinHelper
    {
        Task<string> SetNewSolarCoinAddress(Guid clientId);
    }
}
