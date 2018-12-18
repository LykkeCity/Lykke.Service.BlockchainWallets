using Lykke.Service.BlockchainWallets.Contract.Models;
using Lykke.Service.BlockchainWallets.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core;

namespace Lykke.Service.BlockchainWallets.Controllers
{
    [Route("api/constants")]
    public class ConstantsController : Controller
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly IBlockchainExtensionsService _blockchainExtensionsService;


        public ConstantsController(
            IBlockchainIntegrationService blockchainIntegrationService,
            IBlockchainExtensionsService blockchainExtensionsService)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _blockchainExtensionsService = blockchainExtensionsService;
        }

        /// <summary>
        ///    Returns address extensions constants for the specified blockchain type.
        /// </summary>
        [HttpGet("{blockchainType}/address-extension")]
        public IActionResult GetAddressExtensionConstants(string blockchainType)
        {
            if (string.IsNullOrEmpty(blockchainType))
            {
                return BadRequest
                (
                    BlockchainWalletsErrorResponse.Create($"{nameof(blockchainType)} should not be null or empty.")
                );
            }

            if (blockchainType == LykkeConstants.SolarBlockchainType)
            {
                return Ok(new AddressExtensionConstantsResponse
                {
                    ProhibitedSymbolsForAddressExtension = null,
                    ProhibitedSymbolsForBaseAddress = null,
                    AddressExtensionDisplayName = "",
                    BaseAddressDisplayName = "",
                    TypeForDeposit = AddressExtensionTypeForDeposit.NotSupported,
                    TypeForWithdrawal = AddressExtensionTypeForWithdrawal.NotSupported
                });
            }

            if (!_blockchainIntegrationService.BlockchainIsSupported(blockchainType))
            {
                return BadRequest
                (
                    BlockchainWalletsErrorResponse.Create($"Blockchain type [{blockchainType}] is not supported.")
                );
            }

            var constants = _blockchainExtensionsService.TryGetAddressExtensionConstants(blockchainType);

            return Ok(new AddressExtensionConstantsResponse
            {
                Separator = constants?.SeparatorExists != null ? constants.Separator.ToString() : null,
                ProhibitedSymbolsForAddressExtension = constants?.SeparatorExists != null ? new char[] { constants.Separator } : null,
                ProhibitedSymbolsForBaseAddress = constants?.SeparatorExists != null ? new char[] { constants.Separator } : null,
                AddressExtensionDisplayName = constants?.AddressExtensionDisplayName,
                BaseAddressDisplayName = !string.IsNullOrEmpty(constants?.BaseAddressDisplayName) ? constants.BaseAddressDisplayName : LykkeConstants.PublicAddressExtension.BaseAddressDisplayName,
                TypeForDeposit = constants?.TypeForDeposit ?? AddressExtensionTypeForDeposit.NotSupported,
                TypeForWithdrawal = constants?.TypeForWithdrawal ?? AddressExtensionTypeForWithdrawal.NotSupported
            });
        }
    }
}
