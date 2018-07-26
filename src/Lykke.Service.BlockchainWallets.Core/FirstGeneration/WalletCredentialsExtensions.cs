using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.BlockchainWallets.Contract;

namespace Lykke.Service.BlockchainWallets.Core.FirstGeneration
{
    public static class WalletCredentialsExtensions
    {
        public static string GetDepositAddressForAsset(this IWalletCredentials walletCredentials, string assetId)
        {
            switch (assetId)
            {
                case SpecialAssetIds.BitcoinAssetId:
                    return walletCredentials.MultiSig;
                case SpecialAssetIds.SolarAssetId:
                    return walletCredentials.SolarCoinWalletAddress;
                case SpecialAssetIds.ChronoBankAssetId:
                    return walletCredentials.ChronoBankContract;
                case SpecialAssetIds.QuantaAssetId:
                    return walletCredentials.QuantaContract;
                default:
                    return walletCredentials.ColoredMultiSig;
            }
        }
    }
}
