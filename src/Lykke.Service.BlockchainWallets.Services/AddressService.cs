using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Service.BlockchainWallets.Core.Exceptions;
using Lykke.Service.BlockchainWallets.Core.Services;

namespace Lykke.Service.BlockchainWallets.Services
{
    [UsedImplicitly]
    public class AddressService : IAddressService
    {
        private readonly ICapabilitiesService _capabilitiesService;
        private readonly IConstantsService _constantsService;
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;

        public AddressService(
            ICapabilitiesService capabilitiesService,
            IConstantsService constantsService,
            IBlockchainIntegrationService blockchainIntegrationService)
        {
            _capabilitiesService = capabilitiesService;
            _constantsService = constantsService;
            _blockchainIntegrationService = blockchainIntegrationService;
        }

        public async Task<string> MergeAsync(string blockchainType, string baseAddress, string addressExtension)
        {
            string mergedAddress = null;

            if (string.IsNullOrEmpty(baseAddress))
            {
                throw new OperationException("Base address is empty",
                    OperationErrorCode.BaseAddressIsEmpty);
            }

            var constants = await _constantsService.GetAddressExtensionConstantsAsync(blockchainType);

            if (baseAddress.Contains(constants.Separator))
            {
                throw new OperationException($"Base address should not contain a separator({constants.Separator})",
                    OperationErrorCode.BaseAddressShouldNotContainSeparator);
            }

            if (!string.IsNullOrEmpty(addressExtension))
            {
                if (!await _capabilitiesService.IsPublicAddressExtensionRequiredAsync(blockchainType))
                {
                    throw new NotSupportedException($"Blockchain type [{blockchainType}] is not supported.");
                }

                if (addressExtension.Contains(constants.Separator))
                {
                    throw new OperationException($"Extension address should not contain a separator({constants.Separator})",
                        OperationErrorCode.ExtensionAddressShouldNotContainSeparator);
                }

                mergedAddress = $"{baseAddress}{constants.Separator}{addressExtension}";
            }
            else
            {
                mergedAddress = baseAddress;
            }

            return mergedAddress;
        }

        public async Task<string> GetUnderlyingAddressAsync(string blockchainType, string address)
        {
            if (await _capabilitiesService.IsAddressMappingRequiredAsync(blockchainType))
            {
                var apiClient = _blockchainIntegrationService.GetApiClient(blockchainType);

                var underlyingAddress = await apiClient.GetUnderlyingAddressAsync(address);
                if (!string.IsNullOrWhiteSpace(underlyingAddress))
                {
                    return underlyingAddress;
                }
            }

            return null;
        }

        public async Task<string> GetVirtualAddressAsync(string blockchainType, string address)
        {
            if (await _capabilitiesService.IsAddressMappingRequiredAsync(blockchainType))
            {
                var apiClient = _blockchainIntegrationService.GetApiClient(blockchainType);

                var virtualAddress = await apiClient.GetVirtualAddressAsync(address);
                if (!string.IsNullOrWhiteSpace(virtualAddress))
                {
                    return virtualAddress;
                }
            }

            return null;
        }
    }
}
