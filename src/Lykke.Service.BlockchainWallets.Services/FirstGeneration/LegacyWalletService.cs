using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration;

namespace Lykke.Service.BlockchainWallets.Services.FirstGeneration
{
    public class LegacyWalletService : ILegacyWalletService
    {
        private readonly IFirstGenerationBlockchainWalletRepository _firstGenerationBlockchainWalletRepository;
        private readonly ISrvSolarCoinHelper _srvSolarCoinHelper;
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;
        private readonly ISrvEthereumHelper _srvEthereumHelper;
        private readonly IChronoBankService _chronoBankService;
        private readonly IQuantaService _quantaService;

        public LegacyWalletService(
            IFirstGenerationBlockchainWalletRepository firstGenerationBlockchainWalletRepository,
            ISrvSolarCoinHelper srvSolarCoinHelper,
            IAssetsServiceWithCache assetsServiceWithCache,
            ISrvEthereumHelper srvEthereumHelper,
            IChronoBankService chronoBankService,
            IQuantaService quantaService)
        {
            _firstGenerationBlockchainWalletRepository = firstGenerationBlockchainWalletRepository;
            _srvSolarCoinHelper = srvSolarCoinHelper;
            _assetsServiceWithCache = assetsServiceWithCache;
            _srvEthereumHelper = srvEthereumHelper;
            _chronoBankService = chronoBankService;
            _quantaService = quantaService;
        }

        public async Task CreateWalletAsync(Guid clientId, string assetId, string pubKey, string privateKey)
        {
            switch (assetId)
            {
                #region BTC & ColoredCoins LKK, LKK1y, CHF|USD|EUR|GBP

                #endregion

                #region ETH & ERC20/223

                #endregion

                #region Tree & Time & SLR

                case SpecialAssetIds.SolarAssetId:
                    {
                        var walletCredentials = await _firstGenerationBlockchainWalletRepository.GetAsync(clientId);

                        if (walletCredentials != null)
                        {
                            if (String.IsNullOrEmpty(walletCredentials.SolarCoinWalletAddress))
                            {
                                await _srvSolarCoinHelper.SetNewSolarCoinAddress(walletCredentials);
                            }
                        }
                    }

                    break;

                #endregion

                default:
                    break;
            }
        }

        public async Task GenerateWallet(Guid clientId, SubmitKeysModel request)
        {
            var asset = await _assetsServiceWithCache.TryGetAssetAsync(request.AssetId);
            if (asset == null)
            {
                throw new InvalidOperationException($"Unknown asset {request.AssetId}");
            }

            var (isErc20, bcnRowKey) = await GetAssetInfoAsync(asset);
            var current = await _firstGenerationBlockchainWalletRepository.GetBcnCredsAsync(bcnRowKey, clientId);

            if (current != null)
            {
                throw new InvalidOperationException(
                    $"There is already an entry in bcnCreds for: {request.AssetId}, BcnRowKey: {bcnRowKey}, ClientId: {clientId}");
            }

            string address;

            if (string.IsNullOrEmpty(asset.BlockchainIntegrationLayerId))
            {
                if (request.BcnWallet == null)
                {
                    var walletCreds = await _firstGenerationBlockchainWalletRepository.GetAsync(clientId);
                    address = asset.Blockchain != Lykke.Service.Assets.Client.Models.Blockchain.Ethereum
                        ? (walletCreds.GetDepositAddressForAsset(asset.Id) ??
                          await ObsoleteGenerateAddress(request.AssetId, walletCreds))
                        : null;
                }
                else
                {
                    EthereumResponse<GetContractModel> assetAddress;

                    if (!isErc20)
                    {
                        assetAddress = await _srvEthereumHelper.GetContractAsync(asset.Id,
                            request.BcnWallet.Address);
                    }
                    else
                    {
                        //Get erc20 deposit address here!
                        assetAddress = await _srvEthereumHelper.GetErc20DepositContractAsync(request.BcnWallet.Address);
                    }

                    if (assetAddress.HasError)
                        throw new Exception(assetAddress.Error.ToJson());

                    address = assetAddress.Result.Contract;

                    await _firstGenerationBlockchainWalletRepository.SaveAsync(new BcnCredentialsRecord
                    {
                        Address = request.BcnWallet.Address,
                        AssetAddress = address?.ToLower(),
                        ClientId = clientId.ToString(),
                        EncodedKey = request.BcnWallet.EncodedKey,
                        PublicKey = request.BcnWallet.PublicKey,
                        AssetId = bcnRowKey //Or Asset or Erc20 const
                    });
                }
            }
        }

        private async Task<string> ObsoleteGenerateAddress(string assetId, IWalletCredentials walletCreds)
        {
            string address = null;
            switch (assetId)
            {
                case SpecialAssetIds.SolarAssetId:
                    address = await _srvSolarCoinHelper.SetNewSolarCoinAddress(walletCreds);
                    break;
                case SpecialAssetIds.ChronoBankAssetId:
                    address = await _chronoBankService.SetNewChronoBankContract(walletCreds);
                    break;
                case SpecialAssetIds.QuantaAssetId:
                    address = await _quantaService.SetNewQuantaContract(walletCreds);
                    break;
            }

            return address;
        }

        private async Task<(bool isErc20, string bcnRowKey)> GetAssetInfoAsync(Asset asset)
        {
            bool isAssetErc20 = (asset.Type == AssetType.Erc20Token);
            string bcnRowKey = !isAssetErc20 ? asset.Id : SpecialAssetIds.BcnKeyForErc223;

            return (isAssetErc20, bcnRowKey);
        }
    }
}
