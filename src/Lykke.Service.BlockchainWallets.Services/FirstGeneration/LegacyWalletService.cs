using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration;

namespace Lykke.Service.BlockchainWallets.Services.FirstGeneration
{
    public class LegacyWalletService : ILegacyWalletService
    {
        private readonly IFirstGenerationBlockchainWalletRepository _firstGenerationBlockchainWalletRepository;
        private readonly ISrvSolarCoinHelper _srvSolarCoinHelper;

        public LegacyWalletService(
            IFirstGenerationBlockchainWalletRepository firstGenerationBlockchainWalletRepository,
            ISrvSolarCoinHelper srvSolarCoinHelper)
        {
            _firstGenerationBlockchainWalletRepository = firstGenerationBlockchainWalletRepository;
            _srvSolarCoinHelper = srvSolarCoinHelper;
        }

        public async Task CreateWalletAsync(Guid clientId, string assetId, string pubKey, string privateKey)
        {
            switch (assetId)
            {
                #region Tree & Time & SLR
                case SpecialAssetIds.SolarAssetId:

                    break;
                #endregion

                default:
                    break;
            }
            #region BTC & ColoredCoins LKK, LKK1y, CHF|USD|EUR|GBP

            #endregion

            #region ETH & ERC20/223

            #endregion

            var walletCredentials = await _firstGenerationBlockchainWalletRepository.GetAsync(clientId);

            var solarCoinAddress = "";
            if (walletCredentials != null)
            {
                solarCoinAddress = walletCredentials.SolarCoinWalletAddress
                                   ?? await _srvSolarCoinHelper.SetNewSolarCoinAddress(walletCredentials)
                                   ?? "";
            }
        }
    }
}
