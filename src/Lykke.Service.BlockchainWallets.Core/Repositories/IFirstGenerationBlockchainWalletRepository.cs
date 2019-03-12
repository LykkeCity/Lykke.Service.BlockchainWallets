using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;

namespace Lykke.Service.BlockchainWallets.Core.Repositories
{
    public interface IFirstGenerationBlockchainWalletRepository
    {
        Task<FirstGenerationBlockchainWalletDto> TryGetAsync(string assetId, Guid clientId, bool isErc20, bool isEtherium, bool isColoredCoin);

        Task SetSolarCoinWallet(Guid clientId, string address);

        Task SetChronoBankContract(Guid clientId, string contract);

        Task SetQuantaContract(Guid clientId, string contract);

        Task<IWalletCredentials> GetAsync(Guid clientId);

        Task<IBcnCredentialsRecord> GetBcnCredsAsync(string assetId, Guid clientId);

        Task SaveAsync(IBcnCredentialsRecord credsRecord);

        Task SaveAsync(IWalletCredentials credsRecord);

        Task MergeAsync(IWalletCredentials walletCredentials);

        Task InsertOrReplaceAsync(IBcnCredentialsRecord credsRecord);

        Task DeleteIfExistAsync(IBcnCredentialsRecord credsRecord);

        Task EnumerateBcnCredsByChunksAsync(string assetId, Func<IEnumerable<IBcnCredentialsRecord>, Task> chunks);
    }
}
