using System;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;

namespace Lykke.Service.BlockchainWallets.Core.Repositories
{
    public interface IFirstGenerationBlockchainWalletRepository
    {
        Task<FirstGenerationBlockchainWalletDto> TryGetAsync(string assetId, Guid clientId, bool isErc20, bool isEtherium);

        Task SetSolarCoinWallet(Guid clientId, string address);

        Task<IWalletCredentials> GetAsync(Guid clientId);
    }
}
