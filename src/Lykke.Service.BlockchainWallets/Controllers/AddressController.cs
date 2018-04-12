using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.BlockchainWallets.Controllers
{
    [Route("/api/address")]
    public class AddressController : Controller
    {
        private readonly IAddressService _addressService;
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly ICapabilitiesService _capabilitiesService;


        public AddressController(
            IAddressService addressService,
            IBlockchainIntegrationService blockchainIntegrationService,
            ICapabilitiesService capabilitiesService)
        {
            _addressService = addressService;
            _blockchainIntegrationService = blockchainIntegrationService;
            _capabilitiesService = capabilitiesService;
        }

        [HttpPost("merge")]
        public async Task<IActionResult> MergeAsync([FromBody] MergeAddressRequest request)
        {
            if (!_blockchainIntegrationService.BlockchainIsSupported(request.BlockchainType))
            {
                return BadRequest
                (
                    ErrorResponse.Create($"Blockchain type [{request.BlockchainType}] is not supported.")
                );
            }

            if (!await _capabilitiesService.IsPublicAddressExtensionRequiredAsync(request.BlockchainType))
            {
                return BadRequest
                (
                    ErrorResponse.Create($"Address extension is not supported for specified blockchain type [{request.BlockchainType}].")
                );
            }
            
            return Ok(new MergedAddressResponse
            {
                MergedAddress = await _addressService.MergeAsync
                (
                    blockchainType: request.BlockchainType,
                    publicAddress: request.Address,
                    addressExtension: request.AddressExtension
                )
            });
        }
    }
}
