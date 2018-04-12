using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Models;
using Microsoft.AspNetCore.Mvc;

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


        [HttpGet("{blockchainType}/address-extension")]
        public async Task<IActionResult> GetAddressExtensionConstants(string blockchainType)
        {
            if (!_blockchainIntegrationService.BlockchainIsSupported(blockchainType))
            {
                return BadRequest
                (
                    ErrorResponse.Create($"Blockchain type [{blockchainType}] is not supported.")
                );
            }

            var constants = await _constantsService.GetAddressExtensionConstantsAsync(blockchainType);

            return Ok(new AddressExtensionConstantsResponse
            {
                DisplayName = constants.DisplayName,
                TypeForDeposit = constants.TypeForDeposit,
                TypeForWithdrawal = constants.TypeForWithdrawal
            });
        }
    }
}
