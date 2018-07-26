using System.Collections.Immutable;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Core.Settings.BlockchainIntegrationSettings;

namespace Lykke.Service.BlockchainWallets.Services
{
    [UsedImplicitly]
    public class BlockchainIntegrationService : IBlockchainIntegrationService
    {
        private readonly ImmutableDictionary<string, BlockchainApiClient> _apiClients;


        public BlockchainIntegrationService(
            BlockchainsIntegrationSettings settings,
            ILog log)
        {
            foreach (var blockchain in settings.Blockchains)
            {
                log.WriteInfo
                (
                    "Blockchains registration",
                    "",
                    $"Registering blockchain: {blockchain.Type} -> \r\nAPI: {blockchain.ApiUrl}\r\nHW: {blockchain.HotWalletAddress}"
                );
            }

            _apiClients = settings.Blockchains.ToImmutableDictionary
            (
                x => x.Type,
                y => new BlockchainApiClient(log, y.ApiUrl)
            );
        }

        public async Task<bool> AssetIsSupportedAsync(string blockchainType, string assetId)
        {
            if (blockchainType == SpecialBlockchainTypes.FirstGenerationBlockchain)
                return true;

            var apiClient = TryGetApiClient(blockchainType);

            if (apiClient != null)
            {
                return await apiClient.TryGetAssetAsync(assetId) != null;
            }

            return false;
        }

        public bool BlockchainIsSupported(string blockchainType)
        {
            return TryGetApiClient(blockchainType) != null;
        }

        public IBlockchainApiClient TryGetApiClient(string blockchainType)
        {
            return _apiClients.TryGetValue(blockchainType, out var client)
                ? client
                : null;
        }
    }
}
