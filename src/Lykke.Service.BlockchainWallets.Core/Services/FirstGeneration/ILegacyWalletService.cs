using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration
{
    public interface ILegacyWalletService
    {
        Task<string> CreateWalletAsync(Guid clientId, string assetId);
    }
}
