using System;
using Lykke.Service.BlockchainWallets.MongoRepositories.Wallets.Entities;

namespace Lykke.Service.BlockchainWallets.MongoRepositories.Wallets
{
    internal static class CreatedTypeMappingExtensions
    {
        internal static Contract.CreatorType FromDomain(this CreatorType source)
        {
            switch (source)
            {
                case CreatorType.LykkePay:
                    return Contract.CreatorType.LykkePay;

                case CreatorType.LykkeWallet:
                    return Contract.CreatorType.LykkeWallet;

                default:
                    throw new ArgumentException($"Unknown switch {source}", nameof(source));
            }
        }

        internal static CreatorType ToDomain(this Contract.CreatorType source)
        {
            switch (source)
            {
                case Contract.CreatorType.LykkePay:
                    return CreatorType.LykkePay;

                case Contract.CreatorType.LykkeWallet:
                    return CreatorType.LykkeWallet;

                default:
                    throw new ArgumentException($"Unknown switch {source}", nameof(source));
            }
        }
    }
}
