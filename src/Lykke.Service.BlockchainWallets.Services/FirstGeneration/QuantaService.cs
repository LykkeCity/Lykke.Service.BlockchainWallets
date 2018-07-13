using Common.Log;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;
using Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration;
using System;
using System.Threading.Tasks;
using LkeServices.Generated.QuantaApi.Models;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Services.FirstGeneration.Quanta;
using Lykke.Service.BlockchainWallets.Services.FirstGeneration.Quanta.Models;

namespace Lykke.Service.BlockchainWallets.Services.FirstGeneration
{
    public class QuantaService : IQuantaService
    {
        private readonly IFirstGenerationBlockchainWalletRepository _walletCredentialsRepository;
        private readonly QuantaServiceClientSettings _settings;
        private readonly ILog _log;

        public QuantaService(IFirstGenerationBlockchainWalletRepository walletCredentialsRepository,
            QuantaServiceClientSettings settings, 
            ILog log)
        {
            _walletCredentialsRepository = walletCredentialsRepository;
            _settings = settings;
            _log = log;
        }

        private QuantaApiClient Api => new QuantaApiClient(new Uri(_settings.ServiceUrl));

        public async Task<string> SetNewQuantaContract(IWalletCredentials walletCredentials)
        {
            try
            {
                var contract = (await Api.ApiClientRegisterGetAsync()) as RegisterResponse;

                if (contract != null)
                    await _walletCredentialsRepository.SetQuantaContract(Guid.Parse(walletCredentials.ClientId), contract.Contract);

                return contract?.Contract;
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(QuantaService), nameof(SetNewQuantaContract), "", ex);
            }

            return null;
        }

        public async Task<bool> IsQuantaUser(string address)
        {
            var isQuantaUser = (await Api.ApiClientIsQuantaUserGetAsync(address)) as IsQuantaUserResponse;

            return isQuantaUser?.IsQuantaUser ?? false;
        }
    }
}
