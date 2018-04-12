using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Contract.Constants;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.Services;

namespace Lykke.Service.BlockchainWallets.Services
{
    public class ConstantsService : IConstantsService
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly IDictionary<string, AddressExtensionConstantsDto> _cache;

        public ConstantsService(
            IBlockchainIntegrationService blockchainIntegrationService)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _cache = new Dictionary<string, AddressExtensionConstantsDto>();
        }


        public async Task<AddressExtensionConstantsDto> GetAddressExtensionConstantsAsync(string blockchainType)
        {
            var apiClient = _blockchainIntegrationService.TryGetApiClient(blockchainType);
            if (apiClient == null)
            {
                throw new NotSupportedException($"Blockchain type [{blockchainType}] is not supported.");
            }

            if (!_cache.ContainsKey(blockchainType))
            {
                var constants = await apiClient.GetConstantsAsync();

                if (constants?.PublicAddressExtension != null)
                {
                    _cache[blockchainType] = new AddressExtensionConstantsDto
                    {
                        DisplayName = constants.PublicAddressExtension.DisplayName,
                        Separator = constants.PublicAddressExtension.Separator,
                        TypeForDeposit = AddressExtensionTypeForDeposit.Required,
                        TypeForWithdrawal = AddressExtensionTypeForWithdrawal.Optional
                    };
                }
                else
                {
                    _cache[blockchainType] = new AddressExtensionConstantsDto
                    {
                        TypeForDeposit = AddressExtensionTypeForDeposit.NotSupported,
                        TypeForWithdrawal = AddressExtensionTypeForWithdrawal.NotSupported
                    };
                }
            }
            
            return _cache[blockchainType];
        }
    }
}
