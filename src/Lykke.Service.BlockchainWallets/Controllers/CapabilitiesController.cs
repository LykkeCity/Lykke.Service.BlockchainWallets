using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainWallets.Contract.Models;
using Lykke.Service.BlockchainWallets.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.BlockchainWallets.Controllers
{
    [Route("/api/capabilities")]
    public class CapabilitiesController: Controller
    {

        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly ICapabilitiesService _capabilitiesService;

        public CapabilitiesController(IBlockchainIntegrationService blockchainIntegrationService, 
            ICapabilitiesService capabilitiesService)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _capabilitiesService = capabilitiesService;
        }

        /// <summary>
        ///    Returns capabilities for the specified blockchain type.
        /// </summary>
        [HttpGet("{blockchainType}")]
        public async Task<IActionResult> GetCapabilities(string blockchainType)
        {
            if (string.IsNullOrEmpty(blockchainType))
            {
                return BadRequest
                (
                    ErrorResponse.Create($"{nameof(blockchainType)} should not be null or empty.")
                );
            }

            if (!_blockchainIntegrationService.BlockchainIsSupported(blockchainType))
            {
                return BadRequest
                (
                    ErrorResponse.Create($"Blockchain type [{blockchainType}] is not supported.")
                );
            }

            var isPublicAddressExtensionRequired = await _capabilitiesService.IsPublicAddressExtensionRequiredAsync(blockchainType);

            return Ok(new CapabilititesResponce
            {
                IsPublicAddressExtensionRequired = isPublicAddressExtensionRequired
            });
        }


    }
}
