using System;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;

namespace Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration
{
    public interface ISrvBlockchainHelper
    {
        Task<IBcnCredentialsRecord> GenerateWallets(Guid clientId);
    }
}
