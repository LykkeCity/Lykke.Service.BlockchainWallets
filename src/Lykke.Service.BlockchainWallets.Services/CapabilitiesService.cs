using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.Services;

namespace Lykke.Service.BlockchainWallets.Services
{
    public class CapabilitiesService : ICapabilitiesService
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly IDictionary<string, bool> _cache;

        public CapabilitiesService(
            IBlockchainIntegrationService blockchainIntegrationService)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _cache = new Dictionary<string, bool>();
        }


        public async Task<bool> IsPublicAddressExtensionRequiredAsync(string blockchainType)
        {
            var apiClient = _blockchainIntegrationService.TryGetApiClient(blockchainType);
            if (apiClient == null)
            {
                throw new NotSupportedException($"Blockchain type [{blockchainType}] is not supported.");
            }

            if (!_cache.ContainsKey(blockchainType))
            {
                var capabilities = await apiClient.GetCapabilitiesAsync();

                _cache[blockchainType] = capabilities.IsPublicAddressExtensionRequired;
            }

            return _cache[blockchainType];
            
        }
    }
}
