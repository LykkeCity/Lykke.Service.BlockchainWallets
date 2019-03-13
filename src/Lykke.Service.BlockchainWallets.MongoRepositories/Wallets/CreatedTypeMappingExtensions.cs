using System;
using Lykke.Service.BlockchainWallets;

namespace Lykke.Service.BlockchainWallets.MongoRepositories.Wallets
{
    public static class CreatedTypeMappingExtensions
    {
        public static Contract.CreatorType FromDomain(this WalletMongoEntity.CreatorTypeValues source)
        {
            switch (source)
            {
                case WalletMongoEntity.CreatorTypeValues.LykkePay:
                    return Contract.CreatorType.LykkePay;

                case WalletMongoEntity.CreatorTypeValues.LykkeWallet:
                    return Contract.CreatorType.LykkeWallet;

                default:
                    throw new ArgumentException($"Unknown switch {source}", nameof(source));
            }
        }

        public static WalletMongoEntity.CreatorTypeValues ToDomain(this Contract.CreatorType source)
        {
            switch (source)
            {
                case Contract.CreatorType.LykkePay:
                    return WalletMongoEntity.CreatorTypeValues.LykkePay;

                case Contract.CreatorType.LykkeWallet:
                    return WalletMongoEntity.CreatorTypeValues.LykkeWallet;

                default:
                    throw new ArgumentException($"Unknown switch {source}", nameof(source));
            }
        }
    }
}
