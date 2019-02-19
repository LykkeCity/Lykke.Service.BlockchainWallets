using Lykke.Service.BlockchainWallets;

namespace Lykke.Service.BlockchainWallets.MongoRepositories.Wallets
{
    public static class CreatedTypeMappingExtensions
    {
        public static Contract.CreatorType FromDomain(this WalletMongoEntity.CreatorTypeValues source)
        {
            return (Contract.CreatorType)source;
        }

        public static WalletMongoEntity.CreatorTypeValues ToDomain(this Contract.CreatorType source)
        {
            return (WalletMongoEntity.CreatorTypeValues)source;
        }
    }
}
