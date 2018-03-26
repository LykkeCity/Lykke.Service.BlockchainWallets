using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Castle.DynamicProxy.Generators;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.BlockchainWallets.Controllers
{
    [Route("api/wallets")]
    public class WalletsController : Controller
    {
        public const string _routeSuffix = "{integrationLayerId}/{integrationLayerAssetId}";
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly IWalletService _walletService;


        public WalletsController(
            IBlockchainIntegrationService blockchainIntegrationService,
            IWalletService walletService)
        {
            _walletService = walletService;
            _blockchainIntegrationService = blockchainIntegrationService;
        }


        [HttpPost(_routeSuffix + "/by-client-ids/{clientId}")]
        public async Task<IActionResult> CreateWallet([FromRoute] string integrationLayerId, [FromRoute] string integrationLayerAssetId, [FromRoute] Guid clientId)
        {
            if (!ValidateRequest(integrationLayerId, integrationLayerAssetId, clientId, out var badRequest))
            {
                return badRequest;
            }

            if (!await _blockchainIntegrationService.AssetIsSupportedAsync(integrationLayerId, integrationLayerAssetId))
            {
                return BadRequest
                (
                    ErrorResponse.Create($"Asset [{integrationLayerAssetId}] or/and integration layer [{integrationLayerId}] is not supported.")
                );
            }

            if (await _walletService.DefaultWalletExistsAsync(integrationLayerId, integrationLayerAssetId, clientId))
            {
                return StatusCode
                (
                    (int)HttpStatusCode.Conflict,
                    ErrorResponse.Create($"Wallet for specified client [{clientId}] has already been created.")
                );
            }

            var walletAddress = await _walletService.CreateWalletAsync(integrationLayerId, integrationLayerAssetId, clientId);

            return Ok(new WalletCreatedResponse
            {
                Address = walletAddress
            });
        }

        [HttpDelete(_routeSuffix + "/by-client-ids/{clientId}")]
        public async Task<IActionResult> DeleteWallet([FromRoute] string integrationLayerId, [FromRoute] string integrationLayerAssetId, [FromRoute] Guid clientId)
        {
            if (!ValidateRequest(integrationLayerId, integrationLayerAssetId, clientId, out var badRequest))
            {
                return badRequest;
            }

            if (!await _blockchainIntegrationService.AssetIsSupportedAsync(integrationLayerId, integrationLayerAssetId))
            {
                return BadRequest
                (
                    ErrorResponse.Create($"Asset [{integrationLayerAssetId}] or/and integration layer [{integrationLayerId}] is not supported.")
                );
            }

            if (!await _walletService.WalletExistsAsync(integrationLayerId, integrationLayerAssetId, clientId))
            {
                return NotFound
                (
                    ErrorResponse.Create($"Wallet for specified client [{clientId}] does not exist.")
                );
            }

            await _walletService.DeleteWalletsAsync(integrationLayerId, integrationLayerAssetId, clientId);

            return Accepted();
        }

        [HttpGet("all/by-client-ids/{clientId}")]
        public async Task<IActionResult> GetWallet([FromRoute] Guid clientId, [FromQuery] int take, [FromQuery] string continuationToken)
        {
            if (!ValidateRequest(clientId, out var badRequest))
            {
                return badRequest;
            }

            var (wallets, token) = await _walletService.GetClientWalletsAsync(clientId, take, continuationToken);

            var walletsArray = wallets?.ToList();
            if (walletsArray == null || !walletsArray.Any())
            {
                return NoContent();
            }

            return Ok(new ClientWalletsResponse()
            {
                Wallets = walletsArray.Select(x => new WalletResponse()
                {
                    ClientId = x.ClientId,
                    Address = x.Address,
                    IntegrationLayerAssetId = x.AssetId,
                    IntegrationLayerId = x.IntegrationLayerId
                }),
                ContinuationToken = token
            });
        }

        [HttpGet(_routeSuffix + "/by-client-ids/{clientId}/address")]
        public async Task<IActionResult> GetAddress([FromRoute] string integrationLayerId, [FromRoute] string integrationLayerAssetId, [FromRoute] Guid clientId)
        {
            if (!ValidateRequest(integrationLayerId, integrationLayerAssetId, clientId, out var badRequest))
            {
                return badRequest;
            }

            var address = await _walletService.GetDefaultAddressAsync(integrationLayerId, integrationLayerAssetId, clientId);

            if (string.IsNullOrEmpty(address))
            {
                return NoContent();
            }

            return Ok(new AddressResponse
            {
                Address = address
            });
        }

        [HttpGet(_routeSuffix + "/by-addresses/{address}/client-id")]
        public async Task<IActionResult> GetClientId([FromRoute] string integrationLayerId, [FromRoute] string integrationLayerAssetId, [FromRoute] string address)
        {
            if (!ValidateRequest(integrationLayerId, integrationLayerAssetId, address, out var badRequest))
            {
                return badRequest;
            }

            var clientId = await _walletService.GetClientIdAsync(integrationLayerId, integrationLayerAssetId, address);

            if (!clientId.HasValue)
            {
                return NoContent();
            }

            return Ok(new ClientIdResponse
            {
                ClientId = clientId.Value
            });
        }


        private bool ValidateRequest(string integrationLayerId, string integrationLayerAssetId, string address, out IActionResult badRequest)
        {
            var invalidInputParams = new List<string>();

            if (string.IsNullOrWhiteSpace(integrationLayerId))
            {
                invalidInputParams.Add(nameof(integrationLayerId));
            }

            if (string.IsNullOrWhiteSpace(integrationLayerAssetId))
            {
                invalidInputParams.Add(nameof(integrationLayerAssetId));
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                invalidInputParams.Add(nameof(address));
            }

            if (!invalidInputParams.Any())
            {
                badRequest = null;

                return true;
            }

            badRequest = BadRequest
            (
                ErrorResponse.Create($"One or more input parameters [{string.Join(", ", invalidInputParams)}] are invalid.")
            );

            return false;
        }

        private bool ValidateRequest(string integrationLayerId, string integrationLayerAssetId, Guid clientId,
            out IActionResult badRequest)
        {
            var invalidInputParams = new List<string>();

            if (string.IsNullOrWhiteSpace(integrationLayerId))
            {
                invalidInputParams.Add(nameof(integrationLayerId));
            }

            if (string.IsNullOrWhiteSpace(integrationLayerAssetId))
            {
                invalidInputParams.Add(nameof(integrationLayerAssetId));
            }

            if (clientId == Guid.Empty)
            {
                invalidInputParams.Add(nameof(clientId));
            }

            if (!invalidInputParams.Any())
            {
                badRequest = null;

                return true;
            }

            badRequest = BadRequest
            (
                ErrorResponse.Create($"One or more input parameters [{string.Join(", ", invalidInputParams)}] are invalid.")
            );

            return false;
        }

        private bool ValidateRequest(Guid clientId, out IActionResult badRequest)
        {
            var invalidInputParams = new List<string>();

            if (clientId == Guid.Empty)
            {
                invalidInputParams.Add(nameof(clientId));
            }

            if (!invalidInputParams.Any())
            {
                badRequest = null;

                return true;
            }

            badRequest = BadRequest
            (
                ErrorResponse.Create($"One or more input parameters [{string.Join(", ", invalidInputParams)}] are invalid.")
            );

            return false;
        }
    }
}
