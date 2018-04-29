using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainWallets.Contract.Models;
using Lykke.Service.BlockchainWallets.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.BlockchainWallets.Controllers
{
    public class AddressController : Controller
    {
        private readonly IAddressService _addressService;
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly ICapabilitiesService _capabilitiesService;
        private readonly IAddressParser _addressParser;


        public AddressController(
            IAddressService addressService,
            IBlockchainIntegrationService blockchainIntegrationService,
            ICapabilitiesService capabilitiesService, 
            IAddressParser addressParser)
        {
            _addressService = addressService;
            _blockchainIntegrationService = blockchainIntegrationService;
            _capabilitiesService = capabilitiesService;
            _addressParser = addressParser;
        }

        /// <summary>
        ///    Merges base address with address extension, according to specified blockchain type settings
        /// </summary>
        [HttpGet("/api/{blockchainType}/address/merged/{baseAddress}/{addressExtension}")]
        public async Task<IActionResult> MergeAsync(string blockchainType, string baseAddress, string addressExtension)
        {
            if (!_blockchainIntegrationService.BlockchainIsSupported(blockchainType))
            {
                return BadRequest
                (
                    ErrorResponse.Create($"Blockchain type [{blockchainType}] is not supported.")
                );
            }

            if (!await _capabilitiesService.IsPublicAddressExtensionRequiredAsync(blockchainType))
            {
                return BadRequest
                (
                    ErrorResponse.Create($"Address extension is not supported for specified blockchain type [{blockchainType}].")
                );
            }
            
            return Ok(new MergedAddressResponse
            {
                Address = await _addressService.MergeAsync
                (
                    blockchainType: blockchainType,
                    baseAddress: baseAddress,
                    addressExtension: addressExtension
                )
            });
        }

        /// <summary>
        ///    Extract address parts based on  capabilities for the specified blockchain type.
        /// </summary>
        [HttpGet("/api/{blockchainType}/address/parsed/{address}")]
        public async Task<IActionResult> ParseAddress(string blockchainType, string address)
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

            if (string.IsNullOrEmpty(address))
            {
                return BadRequest
                (
                    ErrorResponse.Create($"{nameof(address)} should not be null or empty.")
                );
            }

            var parseResult = await _addressParser.ExtractAddressParts(blockchainType, address);

            return Ok(new AddressParseResultResponce
            {
                IsPublicAddressExtensionRequired = parseResult.IsPublicAddressExtensionRequired,
                AddressExtension = parseResult.AddressExtension,
                BaseAddress = parseResult.BaseAddress
            });
        }
    }
}
