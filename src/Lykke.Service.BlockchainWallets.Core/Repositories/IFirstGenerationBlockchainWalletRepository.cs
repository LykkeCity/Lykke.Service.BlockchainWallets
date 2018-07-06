using System;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.DTOs;

namespace Lykke.Service.BlockchainWallets.Core.Repositories
{
    public interface IFirstGenerationBlockchainWalletRepository
    {
        Task<FirstGenerationBlockchainWalletDto> TryGetAsync(string assetId, Guid clientId, bool isErc20, bool isEtherium);
    }
}
