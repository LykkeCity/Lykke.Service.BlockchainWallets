using System;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.DTOs;

namespace Lykke.Service.BlockchainWallets.Core.Repositories
{
    public interface IBcnCredentialsWalletRepository
    {
        Task<BcnCredentialsWalletDto> TryGetAsync(string assetId, Guid clientId);
    }
}
