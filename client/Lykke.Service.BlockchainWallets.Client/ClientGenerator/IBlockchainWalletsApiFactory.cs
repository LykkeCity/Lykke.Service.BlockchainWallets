using Lykke.HttpClientGenerator.Caching;
using System.Net.Http;

namespace Lykke.Service.BlockchainWallets.Client.ClientGenerator
{
    public interface IBlockchainWalletsApiFactory
    {
        IBlockchainWalletsApi CreateNew(BlockchainWalletsClientSettings settings,
            bool withCaching = false,
            IClientCacheManager clientCacheManager = null,
            params DelegatingHandler[] handlers);

        IBlockchainWalletsApi CreateNew(string url, bool withCaching = false,
            IClientCacheManager clientCacheManager = null, params DelegatingHandler[] handlers);
    }
}
