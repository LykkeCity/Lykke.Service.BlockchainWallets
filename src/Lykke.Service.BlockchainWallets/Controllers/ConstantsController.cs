using Lykke.Service.BlockchainWallets.Contract.Models;
using Lykke.Service.BlockchainWallets.Core.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Controllers
{
    [Route("api/constants")]
    public class ConstantsController : Controller
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly IConstantsService _constantsService;


        public ConstantsController(
            IBlockchainIntegrationService blockchainIntegrationService,
            IConstantsService constantsService)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _constantsService = constantsService;
        }

        /// <summary>
        ///    Returns address extensions constants for the specified blockchain type.
        /// </summary>
        [HttpGet("{blockchainType}/address-extension")]
        public async Task<IActionResult> GetAddressExtensionConstants(string blockchainType)
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

            var constants = await _constantsService.GetAddressExtensionConstantsAsync(blockchainType);

            return Ok(new AddressExtensionConstantsResponse
            {
                AddressExtensionDisplayName = constants.AddressExtensionDisplayName,
                BaseAddressDisplayName = constants.BaseAddressDisplayName,
                TypeForDeposit = constants.TypeForDeposit,
                TypeForWithdrawal = constants.TypeForWithdrawal
            });
        }
    }
}
