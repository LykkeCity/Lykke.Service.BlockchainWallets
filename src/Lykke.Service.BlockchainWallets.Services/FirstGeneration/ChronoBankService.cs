using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration;
using Lykke.Service.BlockchainWallets.Services.FirstGeneration.ChronoBank;
using Lykke.Service.EthereumCore.Client.Models;

namespace Lykke.Service.BlockchainWallets.Services.FirstGeneration
{
    public class ChronoBankService : IChronoBankService
    {
        private readonly IFirstGenerationBlockchainWalletRepository _walletCredentialsRepository;
        private readonly ChronoBankServiceClientSettings _settings;
        private readonly ILog _log;

        public ChronoBankService(IFirstGenerationBlockchainWalletRepository walletCredentialsRepository,
            ChronoBankServiceClientSettings settings,
            ILog log)
        {
            _walletCredentialsRepository = walletCredentialsRepository;
            _settings = settings;
            _log = log;
        }

        private ChronobankApiClient Api => new ChronobankApiClient(new Uri(_settings.ServiceUrl));

        public async Task<string> SetNewChronoBankContract(IWalletCredentials walletCredentials)
        {
            try
            {
                var contract = (await Api.ApiClientRegisterGetAsync()) as RegisterResponse;

                if (contract != null)
                    await _walletCredentialsRepository.SetChronoBankContract(Guid.Parse(walletCredentials.ClientId), contract.Contract);

                return contract?.Contract;
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(ChronoBankService), nameof(SetNewChronoBankContract), "", ex);
            }

            return null;
        }
    }
}
