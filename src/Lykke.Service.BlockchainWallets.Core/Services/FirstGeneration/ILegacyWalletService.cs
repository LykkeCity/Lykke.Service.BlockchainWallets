using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration
{
    public interface ILegacyWalletService
    {
        Task CreateWalletAsync(Guid clientId, string assetId, string pubKey, string privateKey);
    }
}
