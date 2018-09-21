using Lykke.Service.BlockchainWallets.Contract.Models;
using Lykke.Service.BlockchainWallets.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.BlockchainWallets.Controllers
{
    [Route("/api/capabilities")]
    public class CapabilitiesController: Controller
    {

        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly IBlockchainExtensionsService _blockchainExtensionsService;

        public CapabilitiesController(IBlockchainIntegrationService blockchainIntegrationService, 
            IBlockchainExtensionsService blockchainExtensionsService)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _blockchainExtensionsService = blockchainExtensionsService;
        }

        /// <summary>
        ///    Returns capabilities for the specified blockchain type.
        /// </summary>
        [HttpGet("{blockchainType}")]
        public IActionResult GetCapabilities(string blockchainType)
        {
            if (string.IsNullOrEmpty(blockchainType))
            {
                return BadRequest
                (
                    BlockchainWalletsErrorResponse.Create($"{nameof(blockchainType)} should not be null or empty.")
                );
            }

            if (!_blockchainIntegrationService.BlockchainIsSupported(blockchainType))
            {
                return BadRequest
                (
                    BlockchainWalletsErrorResponse.Create($"Blockchain type [{blockchainType}] is not supported.")
                );
            }

            var isPublicAddressExtensionRequired = _blockchainExtensionsService.IsPublicAddressExtensionRequired(blockchainType);

            return Ok(new CapabilititesResponce
            {
                IsPublicAddressExtensionRequired = isPublicAddressExtensionRequired.HasValue ? isPublicAddressExtensionRequired.Value : false
            });
        }


    }
}
