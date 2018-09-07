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
            var key = $"{blockchainType}-IsPublicAddressExtensionRequired";
            if (_cache.TryGetValue(key, out var value))
            {
                return value;
            }

            var apiClient = _blockchainIntegrationService.GetApiClient(blockchainType);
            var @lock = _locks.GetOrAdd(key, x => new SemaphoreSlim(1));

            await @lock.WaitAsync();

            try
            {
                var capabilities = await apiClient.GetCapabilitiesAsync();
                var result = capabilities.IsPublicAddressExtensionRequired;

                _cache.TryAdd(key, result);

                return result;
            }
            finally
            {
                @lock.Release();
            }
        }

        public async Task<bool> IsAddressMappingRequiredAsync(string blockchainType)
        {
            var key = $"{blockchainType}-IsAddressMappingRequired";
            if (_cache.TryGetValue(key, out var value))
            {
                return value;
            }

            var apiClient = _blockchainIntegrationService.GetApiClient(blockchainType);
            var @lock = _locks.GetOrAdd(key, x => new SemaphoreSlim(1));

            await @lock.WaitAsync();

            try
            {
                var capabilities = await apiClient.GetCapabilitiesAsync();
                var result = capabilities.IsAddressMappingRequired;

                _cache.TryAdd(key, result);

                return result;
            }
            finally
            {
                @lock.Release();
            }
        }
    }
}
