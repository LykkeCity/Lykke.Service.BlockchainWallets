using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Contract.Models;
using Lykke.Service.BlockchainWallets.Core.Services;
using Microsoft.AspNetCore.Mvc;


namespace Lykke.Service.BlockchainWallets.Controllers
{
    [Route("api/wallets")]
    public class WalletsController : Controller
    {
        private const string RouteSuffix = "{blockchainType}/{assetId}";
        
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly IWalletService _walletService;


        public WalletsController(
            IBlockchainIntegrationService blockchainIntegrationService,
            IWalletService walletService)
        {
            _walletService = walletService;
            _blockchainIntegrationService = blockchainIntegrationService;
        }

        /// <summary>
        ///    Creates wallet for the specified client in the specified blockchain/asset pair.
        /// </summary>
        /// <remarks>
        ///    walletType reserved for future use.
        /// </remarks>
        [HttpPost(RouteSuffix + "/by-client-ids/{clientId}")]
        public async Task<IActionResult> CreateWallet([FromRoute] string blockchainType, [FromRoute] string assetId, [FromRoute] Guid clientId, [FromQuery] WalletType? walletType)
        {
            if (!ValidateRequest(blockchainType, assetId, clientId, out var badRequest))
            {
                return badRequest;
            }

            if (!await _blockchainIntegrationService.AssetIsSupportedAsync(blockchainType, assetId))
            {
                return BadRequest
                (
                    ErrorResponse.Create($"Asset [{assetId}] or/and blockchain type [{blockchainType}] is not supported.")
                );
            }

            if (await _walletService.DefaultWalletExistsAsync(blockchainType, assetId, clientId))
            {
                return StatusCode
                (
                    (int)HttpStatusCode.Conflict,
                    ErrorResponse.Create($"Wallet for specified client [{clientId}] has already been created.")
                );
            }

            var wallet = await _walletService.CreateWalletAsync(blockchainType, assetId, clientId);

            return Ok(new WalletResponse
            {
                Address = wallet.Address,
                AddressExtension = wallet.AddressExtension,
                BaseAddress = wallet.BaseAddress,
                BlockchainType = wallet.BlockchainType,
                ClientId = wallet.ClientId,
                IntegrationLayerId = wallet.BlockchainType,
                IntegrationLayerAssetId = wallet.AssetId
            });
        }

        /// <summary>
        ///    Removes wallet for the specified client in the specified blockchain type/asset pair
        /// </summary>
        [HttpDelete(RouteSuffix + "/by-client-ids/{clientId}")]
        public async Task<IActionResult> DeleteWallet([FromRoute] string blockchainType, [FromRoute] string assetId, [FromRoute] Guid clientId)
        {
            if (!ValidateRequest(blockchainType, assetId, clientId, out var badRequest))
            {
                return badRequest;
            }

            if (!await _blockchainIntegrationService.AssetIsSupportedAsync(blockchainType, assetId))
            {
                return BadRequest
                (
                    ErrorResponse.Create($"Asset [{assetId}] or/and blockchain type [{blockchainType}] is not supported.")
                );
            }

            if (!await _walletService.WalletExistsAsync(blockchainType, assetId, clientId))
            {
                return NotFound
                (
                    ErrorResponse.Create($"Wallet for specified client [{clientId}] does not exist.")
                );
            }

            await _walletService.DeleteWalletsAsync(blockchainType, assetId, clientId);

            return Accepted();
        }
        
        /// <summary>
        ///    Returns wallet address for the specified client in the specified blockchain type/asset pair.
        /// </summary>
        [HttpGet(RouteSuffix + "/by-client-ids/{clientId}/address")]
        public async Task<IActionResult> GetAddress([FromRoute] string blockchainType, [FromRoute] string assetId, [FromRoute] Guid clientId)
        {
            if (!ValidateRequest(blockchainType, assetId, clientId, out var badRequest))
                return badRequest;

            var address = blockchainType == SpecialBlockchainTypes.FirstGenerationBlockchain
                ? await _walletService.TryGetFirstGenerationBlockchainAddressAsync(assetId, clientId)
                : await _walletService.TryGetDefaultAddressAsync(blockchainType, assetId, clientId);

            if (address != null)
            {
                return Ok(new AddressResponse
                {
                    Address = address.Address,
                    AddressExtension = address.AddressExtension,
                    BaseAddress = address.BaseAddress
                });
            }
            else
            {
                return NoContent();
            }
        }

        /// <summary>
        ///    Return client id for the specified wallet.
        /// </summary>
        [HttpGet(RouteSuffix + "/by-addresses/{address}/client-id")]
        public async Task<IActionResult> GetClientId([FromRoute] string blockchainType, [FromRoute] string assetId, [FromRoute] string address)
        {
            if (!ValidateRequest(blockchainType, assetId, address, out var badRequest))
            {
                return badRequest;
            }

            var clientId = await _walletService.TryGetClientIdAsync(blockchainType, assetId, address);

            if (clientId != null)
            {
                return Ok(new ClientIdResponse
                {
                    ClientId = clientId.Value
                });
            }
            else
            {
                return NoContent();
            }
        }

        /// <summary>
        ///    Returns all wallets for the specified client.
        /// </summary>
        [HttpGet("all/by-client-ids/{clientId}")]
        public async Task<IActionResult> GetWallets([FromRoute] Guid clientId, [FromQuery] int take, [FromQuery] string continuationToken)
        {
            if (take <= 0)
            {
                return BadRequest(ErrorResponse.Create($"{nameof(take)} should be greater then 0."));
            }

            if (!ValidateRequest(clientId, out var badRequest))
            {
                return badRequest;
            }

            var (wallets, token) = await _walletService.GetClientWalletsAsync(clientId, take, continuationToken);

            var response = new WalletsResponse
            {
                Wallets = wallets.Select(x => new WalletResponse
                {
                    Address = x.Address,
                    AddressExtension = x.AddressExtension,
                    BaseAddress = x.BaseAddress,
                    BlockchainType = x.BlockchainType,
                    ClientId = x.ClientId,
                    IntegrationLayerId = x.BlockchainType,
                    IntegrationLayerAssetId = x.AssetId
                }),
                ContinuationToken = token
            };

            if (response.Wallets.Any())
            {
                return Ok(response);
            }
            else
            {
                return NoContent();
            }
        }


        private bool ValidateRequest(string blockchainType, string assetId, string address, out IActionResult badRequest)
        {
            var invalidInputParams = new List<string>();

            if (string.IsNullOrWhiteSpace(blockchainType))
            {
                invalidInputParams.Add(nameof(blockchainType));
            }

            if (string.IsNullOrWhiteSpace(assetId))
            {
                invalidInputParams.Add(nameof(assetId));
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

        private bool ValidateRequest(string blockchainType, string assetId, Guid clientId, out IActionResult badRequest)
        {
            var invalidInputParams = new List<string>();

            if (string.IsNullOrWhiteSpace(blockchainType))
            {
                invalidInputParams.Add(nameof(blockchainType));
            }

            if (string.IsNullOrWhiteSpace(assetId))
            {
                invalidInputParams.Add(nameof(assetId));
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
