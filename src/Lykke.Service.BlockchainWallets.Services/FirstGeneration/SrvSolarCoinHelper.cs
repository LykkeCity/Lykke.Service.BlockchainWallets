using Common;
using Common.Log;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Lykke.Common.Log;

namespace Lykke.Service.BlockchainWallets.Services.FirstGeneration
{
    public class SrvSolarCoinHelper : ISrvSolarCoinHelper
    {
        private readonly SolarCoinServiceClientSettings _solarCoinSettings;
        private readonly ILog _log;
        private readonly IFirstGenerationBlockchainWalletRepository _walletCredentialsRepository;
        private readonly HttpClient _httpClient;

        public SrvSolarCoinHelper(SolarCoinServiceClientSettings solarCoinSettings, ILogFactory logFactory,
            IFirstGenerationBlockchainWalletRepository walletCredentialsRepository)
        {
            if (logFactory == null)
                throw new ArgumentNullException(nameof(logFactory));
            _log = logFactory.CreateLog(this);

            _httpClient = new HttpClient();
            _solarCoinSettings = solarCoinSettings;
            _walletCredentialsRepository = walletCredentialsRepository ?? throw new ArgumentNullException(nameof(walletCredentialsRepository));
        }

        public async Task<string> SetNewSolarCoinAddress(Guid clientId)
        {
            try
            {
                var rawResponse = await _httpClient.GetAsync(_solarCoinSettings.ServiceUrl);
                var address = (await rawResponse.Content.ReadAsStringAsync()).DeserializeJson<GetAddressModel>().Address;

                await _walletCredentialsRepository.SetSolarCoinWallet(clientId, address);

                return address;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }

            return null;
        }
    }

    #region Response Models

    public class GetAddressModel
    {
        public string Address { get; set; }
    }

    #endregion
}
