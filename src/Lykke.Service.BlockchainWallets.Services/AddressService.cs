using System;
using System.Linq;
using System.Text;
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


        public AddressService(
            ICapabilitiesService capabilitiesService,
            IConstantsService constantsService)
        {
            _capabilitiesService = capabilitiesService;
            _constantsService = constantsService;
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
    }
}
