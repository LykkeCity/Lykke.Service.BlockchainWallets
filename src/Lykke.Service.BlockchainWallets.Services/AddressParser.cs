using JetBrains.Annotations;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.Services;

namespace Lykke.Service.BlockchainWallets.Services
{
    [UsedImplicitly]
    public class AddressParser : IAddressParser
    {
        private readonly IBlockchainExtensionsService _extensionsService;

        public AddressParser(
            IBlockchainExtensionsService extensionsService)
        {
            _extensionsService = extensionsService;
        }

        public AddressParseResultDto ExtractAddressParts(string blockchainType, string address)
        {
            var addressExtension = string.Empty;
            var baseAddress = string.Empty;
            var isPublicAddressExtensionRequired = false;
            
            var constants = _extensionsService.TryGetAddressExtensionConstants(blockchainType);

            if (constants != null)
            {
                var addressAndExtension = address.Split(constants.Separator, 2);

                isPublicAddressExtensionRequired = true;
                baseAddress = addressAndExtension[0];

                if (addressAndExtension.Length == 2)
                    addressExtension = addressAndExtension[1];
            }

            return new AddressParseResultDto
            {
                AddressExtension = addressExtension,
                BaseAddress = baseAddress,
                IsPublicAddressExtensionRequired = isPublicAddressExtensionRequired
            };
        }
    }
}
