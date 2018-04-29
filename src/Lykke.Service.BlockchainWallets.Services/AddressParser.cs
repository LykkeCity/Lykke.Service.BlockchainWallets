using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.Services;

namespace Lykke.Service.BlockchainWallets.Services
{
    public class AddressParser:IAddressParser
    {
        private readonly ICapabilitiesService _capabilitiesService;
        private readonly IConstantsService _constantsService;

        public AddressParser(ICapabilitiesService capabilitiesService, IConstantsService constantsService)
        {
            _capabilitiesService = capabilitiesService;
            _constantsService = constantsService;
        }


        public async Task<AddressParseResultDto> ExtractAddressParts(string blockchainType, string address)
        {
            var addressExtension = string.Empty;
            var baseAddress = string.Empty;
            var isPublicAddressExtensionRequired = false;

            if (await _capabilitiesService.IsPublicAddressExtensionRequiredAsync(blockchainType))
            {
                var constants = await _constantsService.GetAddressExtensionConstantsAsync(blockchainType);

                var addressAndExtension = address.Split(constants.Separator, 2);

                isPublicAddressExtensionRequired = true;
                baseAddress = addressAndExtension[0];

                if (addressAndExtension.Length == 2)
                {
                    addressExtension = addressAndExtension[1];
                }
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
