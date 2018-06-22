using System;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Contract.Models;
using Lykke.Service.BlockchainWallets.Core.Exceptions;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Extensions;
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

        [HttpGet("/api/{blockchainType}/address/merged/{baseAddress}")]
        public async Task<IActionResult> MergeAsync(string blockchainType, string baseAddress)
        {
            return await MergeAddressAsync(blockchainType, baseAddress, null);
        }
        /// <summary>
        ///    Merges base address with address extension, according to specified blockchain type settings
        /// </summary>

        [HttpGet("/api/{blockchainType}/address/merged/{baseAddress}/{addressExtension}")]
        public async Task<IActionResult> MergeAsync(string blockchainType, string baseAddress, string addressExtension)
        {
            return await MergeAddressAsync(blockchainType, baseAddress, addressExtension);
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

            if (string.IsNullOrEmpty(address))
            {
                return BadRequest
                (
                    BlockchainWalletsErrorResponse.Create($"{nameof(address)} should not be null or empty.")
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

        private async Task<IActionResult> MergeAddressAsync(string blockchainType, string baseAddress, string addressExtension)
        {
            if (!_blockchainIntegrationService.BlockchainIsSupported(blockchainType))
            {
                return BadRequest
                (
                    BlockchainWalletsErrorResponse.Create($"Blockchain type [{blockchainType}] is not supported.")
                );
            }

            if (!await _capabilitiesService.IsPublicAddressExtensionRequiredAsync(blockchainType))
            {
                return BadRequest
                (
                    BlockchainWalletsErrorResponse.Create(
                        $"Address extension is not supported for specified blockchain type [{blockchainType}].")
                );
            }

            try
            {
                var address = await _addressService.MergeAsync
                (
                    blockchainType: blockchainType,
                    baseAddress: baseAddress,
                    addressExtension: addressExtension
                );

                return Ok(new MergedAddressResponse
                {
                    Address = address
                });
            }
            catch (OperationException e)
            {
                return BadRequest
                (
                    BlockchainWalletsErrorResponse.Create(
                        e.Message,
                        e.ErrorCode.ToErrorCodeType())
                );
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
