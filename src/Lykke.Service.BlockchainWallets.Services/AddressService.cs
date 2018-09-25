using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Service.BlockchainWallets.Core.Exceptions;
using Lykke.Service.BlockchainWallets.Core.Services;

namespace Lykke.Service.BlockchainWallets.Services
{
    [UsedImplicitly]
    public class AddressService : IAddressService
    {
        private readonly IBlockchainExtensionsService _blockchainExtensionsService;
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly ILog _log;

        public AddressService(
            IBlockchainExtensionsService blockchainExtensionsService,
            IBlockchainIntegrationService blockchainIntegrationService,
            ILogFactory logFactory)
        {
            _blockchainExtensionsService = blockchainExtensionsService;
            _blockchainIntegrationService = blockchainIntegrationService;

            _log = logFactory.CreateLog(this);
        }

        public string Merge(string blockchainType, string baseAddress, string addressExtension)
        {
            string mergedAddress = null;

            if (string.IsNullOrEmpty(baseAddress))
            {
                throw new OperationException("Base address is empty",
                    OperationErrorCode.BaseAddressIsEmpty);
            }

            var constants = _blockchainExtensionsService.TryGetAddressExtensionConstants(blockchainType);

            if (baseAddress.Contains(constants.Separator))
            {
                throw new OperationException($"Base address should not contain a separator({constants.Separator})",
                    OperationErrorCode.BaseAddressShouldNotContainSeparator);
            }

            if (!string.IsNullOrEmpty(addressExtension))
            {
                var capabilityQueryResult = _blockchainExtensionsService.IsPublicAddressExtensionRequired(blockchainType);
                if (!capabilityQueryResult.HasValue)
                {
                    throw new InvalidOperationException($"API service for blockchain type [{blockchainType}] is not currently available.");
                }
                if (!capabilityQueryResult.Value)
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
            var capabilityQueryResult = _blockchainExtensionsService.IsAddressMappingRequired(blockchainType);
            if (capabilityQueryResult ?? false)
            {
                var apiClient = _blockchainIntegrationService.GetApiClient(blockchainType);

                try
                {
                    var underlyingAddress = await apiClient.GetUnderlyingAddressAsync(address);
                    if (!string.IsNullOrWhiteSpace(underlyingAddress))
                    {
                        return underlyingAddress;
                    }
                }
                catch (Exception ex) when (!(ex is ArgumentException)) // BlockchainAPiClient thows ArgumentException when input parameter validation's failed.
                {
                    _log.Warning($"Unable to access API service for blockchain {blockchainType}.", ex);
                }
            }

            return null;
        }

        public async Task<string> GetVirtualAddressAsync(string blockchainType, string address)
        {
            var capabilityQueryResult = _blockchainExtensionsService.IsAddressMappingRequired(blockchainType);
            if (capabilityQueryResult ?? false)
            {
                var apiClient = _blockchainIntegrationService.GetApiClient(blockchainType);

                try
                {
                    var virtualAddress = await apiClient.GetVirtualAddressAsync(address);
                    if (!string.IsNullOrWhiteSpace(virtualAddress))
                    {
                        return virtualAddress;
                    }
                }
                catch (Exception ex) when (!(ex is ArgumentException)) // BlockchainAPiClient thows ArgumentException when input parameter validation's failed.
                {
                    _log.Warning($"Unable to access API service for blockchain {blockchainType}.", ex);
                }
            }

            return null;
        }
    }
}
