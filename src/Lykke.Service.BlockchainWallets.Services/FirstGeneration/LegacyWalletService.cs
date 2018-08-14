using Common;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration;
using System;
using System.Threading.Tasks;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;

namespace Lykke.Service.BlockchainWallets.Services.FirstGeneration
{
    public class LegacyWalletService : ILegacyWalletService
    {
        private readonly IFirstGenerationBlockchainWalletRepository _firstGenerationBlockchainWalletRepository;
        private readonly ISrvSolarCoinHelper _srvSolarCoinHelper;
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;
        private readonly ISrvEthereumHelper _srvEthereumHelper;
        private readonly ISrvBlockchainHelper _srvBlockchainHelper;

        public LegacyWalletService(
            IFirstGenerationBlockchainWalletRepository firstGenerationBlockchainWalletRepository,
            ISrvSolarCoinHelper srvSolarCoinHelper,
            IAssetsServiceWithCache assetsServiceWithCache,
            ISrvEthereumHelper srvEthereumHelper,
            ISrvBlockchainHelper srvBlockchainHelper)
        {
            _firstGenerationBlockchainWalletRepository = firstGenerationBlockchainWalletRepository;
            _srvSolarCoinHelper = srvSolarCoinHelper;
            _assetsServiceWithCache = assetsServiceWithCache;
            _srvEthereumHelper = srvEthereumHelper;
            _srvBlockchainHelper = srvBlockchainHelper;
        }

        public async Task<string> CreateWalletAsync(Guid clientId, string assetId)
        {
            var asset = await _assetsServiceWithCache.TryGetAssetAsync(assetId);

            #region BTC & ColoredCoins LKK, LKK1y, CHF|USD|EUR|GBP
            bool isColored = !string.IsNullOrEmpty(asset.BlockChainAssetId) &&
                             asset.Blockchain == Blockchain.Bitcoin;
            if (assetId != SpecialAssetIds.SolarAssetId &&
                (assetId == SpecialAssetIds.BitcoinAssetId ||
                 isColored))
            {
                var bcncreds = await _srvBlockchainHelper.GenerateWallets(clientId);

                if (isColored)
                {
                    var wallet = await _firstGenerationBlockchainWalletRepository.GetAsync(clientId);

                    return wallet.ColoredMultiSig;
                }

                return bcncreds.AssetAddress;
            }

            #endregion

            #region ETH(In future) & ERC20/223 & Tree & Time & SLR

            var address = await GenerateWallet(clientId, assetId);
            return address;

            #endregion
        }

        public async Task<string> GenerateWallet(Guid clientId, string assetId)
        {
            var asset = await _assetsServiceWithCache.TryGetAssetAsync(assetId);
            if (asset == null)
            {
                throw new InvalidOperationException($"Unknown asset {assetId}");
            }

            var (isErc20, bcnRowKey) = await GetAssetInfoAsync(asset);
            var current = await _firstGenerationBlockchainWalletRepository.GetBcnCredsAsync(bcnRowKey, clientId);

            if (current != null)
            {
                return current.AssetAddress;
            }

            string address = null;

            if (string.IsNullOrEmpty(asset.BlockchainIntegrationLayerId))
            {
                var walletCreds = await _firstGenerationBlockchainWalletRepository.GetAsync(clientId);
                address = asset.Blockchain != Lykke.Service.Assets.Client.Models.Blockchain.Ethereum
                    ? (walletCreds?.GetDepositAddressForAsset(asset.Id) ??
                      await ObsoleteGenerateAddress(assetId, clientId))
                    : null;
                if (address == null)
                {
                    EthereumResponse<GetContractModel> assetAddress;

                    #region Generate ETH key for erc223 deposit address

                    var key = Nethereum.Signer.EthECKey.GenerateKey();
                    var publicAddress = key.GetPublicAddress().ToLower();

                    #endregion

                    if (!isErc20)
                    {
                        throw new NotImplementedException("Can't create ETH deposit yet");
                        //assetAddress = await _srvEthereumHelper.GetContractAsync(asset.Id,
                        //    request.BcnWallet.Address);
                    }
                    else
                    {
                        //Get erc20 deposit address here!
                        assetAddress = await _srvEthereumHelper.GetErc20DepositContractAsync(publicAddress);
                    }

                    if (assetAddress.HasError)
                        throw new Exception(assetAddress.Error.ToJson());

                    address = assetAddress.Result.Contract;

                    await _firstGenerationBlockchainWalletRepository.SaveAsync(new BcnCredentialsRecord
                    {
                        Address = publicAddress,
                        AssetAddress = address?.ToLower(),
                        ClientId = clientId.ToString(),
                        EncodedKey = "",//request.BcnWallet.EncodedKey,
                        PublicKey = "",//request.BcnWallet.PublicKey,
                        AssetId = bcnRowKey //Or Asset or Erc20 const
                    });
                }
            }

            return address;
        }

        private async Task<string> ObsoleteGenerateAddress(string assetId, Guid clientId)
        {
            string address = null;
            switch (assetId)
            {
                case SpecialAssetIds.SolarAssetId:
                    address = await _srvSolarCoinHelper.SetNewSolarCoinAddress(clientId);
                    break;
                // Assets below are no longer supported on production
                //case SpecialAssetIds.ChronoBankAssetId:
                //    address = await _chronoBankService.SetNewChronoBankContract(walletCreds);
                //    break;
                //case SpecialAssetIds.QuantaAssetId:
                //    address = await _quantaService.SetNewQuantaContract(walletCreds);
                //    break;
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
