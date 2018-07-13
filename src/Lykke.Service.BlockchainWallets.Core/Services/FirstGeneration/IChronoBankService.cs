using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;

namespace Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration
{
    public interface IChronoBankService
    {
        Task<string> SetNewChronoBankContract(IWalletCredentials walletCredentials);
    }
}
