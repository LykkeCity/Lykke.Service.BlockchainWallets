using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Service.BlockchainWallets.Core.Services;

namespace Lykke.Service.BlockchainWallets.Services
{
    [UsedImplicitly]
    public class CapabilitiesService : ICapabilitiesService
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly ConcurrentDictionary<string, bool> _cache;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;

        public CapabilitiesService(
            IBlockchainIntegrationService blockchainIntegrationService)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _cache = new ConcurrentDictionary<string, bool>();
            _locks = new ConcurrentDictionary<string, SemaphoreSlim>();
        }


        public async Task<bool> IsPublicAddressExtensionRequiredAsync(string blockchainType)
        {
            if (string.IsNullOrEmpty(blockchainType))
            {
                throw new ArgumentException("Should not be null or empty", nameof(blockchainType));
            }

            var apiClient = _blockchainIntegrationService.TryGetApiClient(blockchainType);
            if (apiClient == null)
            {
                throw new NotSupportedException($"Blockchain type [{blockchainType}] is not supported.");
            }

            bool result;

            var @lock = _locks.GetOrAdd(blockchainType, x => new SemaphoreSlim(1));

            await @lock.WaitAsync();

            try
            {
                var capabilities = await apiClient.GetCapabilitiesAsync();

                result = capabilities.IsPublicAddressExtensionRequired;

                _cache.TryAdd(blockchainType, result);
            }
            finally
            {
                @lock.Release();
            }

            return result;
            
        }
    }
}
