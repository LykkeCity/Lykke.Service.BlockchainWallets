using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Core.Settings.BlockchainIntegrationSettings;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Lykke.Service.BlockchainWallets.Services
{
    [UsedImplicitly]
    public class BlockchainIntegrationService : IBlockchainIntegrationService
    {
        private readonly ImmutableDictionary<string, BlockchainApiClient> _apiClients;
        private readonly ImmutableDictionary<string, BlockchainSettings> _blockchainSettings;

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

            _blockchainSettings = settings.Blockchains.ToImmutableDictionary(x => x.Type,
                y => new BlockchainSettings()
                {
                    Type = y.Type,
                    ApiUrl = y.ApiUrl,
                    HotWalletAddress = y.HotWalletAddress,
                });

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

        public BlockchainSettings GetSettings(string blockchainType)
        {
            if (string.IsNullOrEmpty(blockchainType))
            {
                throw new ArgumentException("Should not be null or empty", nameof(blockchainType));
            }

            _blockchainSettings.TryGetValue(blockchainType, out var settings);
            if (settings == null)
            {
                throw new NotSupportedException($"Blockchain type [{blockchainType}] is not supported.");
            }

            return settings;
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
