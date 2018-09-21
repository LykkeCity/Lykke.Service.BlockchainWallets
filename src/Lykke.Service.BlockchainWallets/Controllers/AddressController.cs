using Lykke.Service.BlockchainWallets.Contract.Models;
using Lykke.Service.BlockchainWallets.Core;
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
        private readonly IBlockchainExtensionsService _blockchainExtensionsService;
        private readonly IAddressParser _addressParser;


        public AddressController(
            IAddressService addressService,
            IBlockchainIntegrationService blockchainIntegrationService,
            IBlockchainExtensionsService blockchainExtensionsService,
            IAddressParser addressParser)
        {
            _addressService = addressService;
            _blockchainIntegrationService = blockchainIntegrationService;
            _blockchainExtensionsService = blockchainExtensionsService;
            _addressParser = addressParser;
        }

        [HttpGet("/api/{blockchainType}/address/merged/{baseAddress}")]
        public IActionResult MergeAsync(string blockchainType, string baseAddress)
        {
            return MergeAddress(blockchainType, baseAddress, null);
        }
        /// <summary>
        ///    Merges base address with address extension, according to specified blockchain type settings
        /// </summary>

        [HttpGet("/api/{blockchainType}/address/merged/{baseAddress}/{addressExtension}")]
        public IActionResult MergeAsync(string blockchainType, string baseAddress, string addressExtension)
        {
            return MergeAddress(blockchainType, baseAddress, addressExtension);
        }

        /// <summary>
        ///    Extract address parts based on  capabilities for the specified blockchain type.
        /// </summary>
        [HttpGet("/api/{blockchainType}/address/parsed/{address}")]
        public IActionResult ParseAddress(string blockchainType, string address)
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

            var parseResult = _addressParser.ExtractAddressParts(blockchainType, address);

            return Ok(new AddressParseResultResponce
            {
                IsPublicAddressExtensionRequired = parseResult.IsPublicAddressExtensionRequired,
                AddressExtension = parseResult.AddressExtension,
                BaseAddress = parseResult.BaseAddress
            });
        }

        private IActionResult MergeAddress(string blockchainType, string baseAddress, string addressExtension)
        {
            if (blockchainType == LykkeConstants.SolarBlockchainType)
            {
                return Ok(new MergedAddressResponse
                {
                    Address = baseAddress
                });
            }

            if (!_blockchainIntegrationService.BlockchainIsSupported(blockchainType))
            {
                return BadRequest
                (
                    BlockchainWalletsErrorResponse.Create($"Blockchain type [{blockchainType}] is not supported.")
                );
            }

            try
            {
                var address = _addressService.Merge
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
        }
    }
}
