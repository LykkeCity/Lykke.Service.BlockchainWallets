using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Log;
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
            int timeoutFoApiInSeconds,
            ILogFactory logFactory)
        {
            if (logFactory == null)
                throw new ArgumentNullException(nameof(logFactory));
            var log = logFactory.CreateLog(this);
            var timeout = TimeSpan.FromSeconds(timeoutFoApiInSeconds);

            foreach (var blockchain in settings.Blockchains)
            {
                log.Info($"Registering blockchain: {blockchain.Type} -> \r\nAPI: {blockchain.ApiUrl}\r\nHW: {blockchain.HotWalletAddress}");
            }

            _apiClients = settings.Blockchains.ToImmutableDictionary
            (
                x => x.Type,
                y => new BlockchainApiClient(logFactory, y.ApiUrl, timeout, 3)
            );
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

        public IBlockchainApiClient GetApiClient(string blockchainType)
        {
            if (string.IsNullOrEmpty(blockchainType))
            {
                throw new ArgumentException("Should not be null or empty", nameof(blockchainType));
            }

            var apiClient = TryGetApiClient(blockchainType);
            if (apiClient == null)
            {
                throw new NotSupportedException($"Blockchain type [{blockchainType}] is not supported.");
            }

            return apiClient;
        }

        public ImmutableDictionary<string, BlockchainApiClient>.Enumerator GetApiClientsEnumerator()
        {
            return _apiClients.GetEnumerator();
        }

        public IEnumerable<KeyValuePair<string, BlockchainApiClient>> GetApiClientsEnumerable()
        {
            return _apiClients.ToImmutableArray();
        }
    }
}
