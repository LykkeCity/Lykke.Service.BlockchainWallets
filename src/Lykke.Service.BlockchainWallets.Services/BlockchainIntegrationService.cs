using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainSignService.Client;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Core.Settings.BlockchainIntegrationSettings;


namespace Lykke.Service.BlockchainWallets.Services
{
    public class BlockchainIntegrationService : IBlockchainIntegrationService
    {
        private readonly ImmutableDictionary<string, BlockchainApiClient>         _apiClients;
        private readonly ImmutableDictionary<string, BlockchainSignServiceClient> _signServiceClients;


        public BlockchainIntegrationService(
            BlockchainsIntegrationSettings settings,
            ILog log)
        {
            _apiClients = settings.Blockchains.ToImmutableDictionary
            (
                x => x.Type,
                y => new BlockchainApiClient(y.ApiUrl)
            );

            _signServiceClients = settings.Blockchains.ToImmutableDictionary
            (
                x => x.Type,
                y => new BlockchainSignServiceClient(y.SignFacadeUrl, log)
            );
        }

        public async Task<bool> AssetIsSupported(string integrationLayerId, string assetId)
        {
            var apiClient = GetApiClient(integrationLayerId);

            if (apiClient != null)
            {
                return await apiClient.TryGetAssetAsync(assetId) != null;
            }

            return false;
        }

        public IBlockchainApiClient GetApiClient(string integrationLayerId)
        {
            return _apiClients.TryGetValue(integrationLayerId, out var client) 
                 ? client
                 : null;
        }

        public IBlockchainSignServiceClient GetSignServiceClient(string integrationLayerId)
        {
            return _signServiceClients.TryGetValue(integrationLayerId, out var client)
                 ? client
                 : null;
        }
    }
}
