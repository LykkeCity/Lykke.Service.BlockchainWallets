﻿using System;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.Services;

namespace Lykke.Service.BlockchainWallets.Services
{
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


        public async Task<string> MergeAsync(string blockchainType, string publicAddress, string addressExtension)
        {
            if (!await _capabilitiesService.IsPublicAddressExtensionRequiredAsync(blockchainType))
            {
                throw new NotSupportedException($"Blockchain type [{blockchainType}] is not supported.");
            }

            var constants = await _constantsService.GetAddressExtensionConstantsAsync(blockchainType);

            return $"{publicAddress}{constants.Separator}{addressExtension}";
        }
    }
}
