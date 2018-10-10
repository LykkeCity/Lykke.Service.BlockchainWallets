using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Contract.Models;
using Lykke.Service.BlockchainWallets.Core.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common;
using Lykke.Service.BlockchainWallets.Enums;


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
        [Obsolete]
        [HttpPost(RouteSuffix + "/by-client-ids/{clientId}")]
        public async Task<IActionResult> CreateWallet([FromRoute] string blockchainType, [FromRoute] string assetId, [FromRoute] Guid clientId, [FromQuery] WalletType? walletType)
        {
            blockchainType = blockchainType.TrimAllSpacesAroundNullSafe();
            assetId = assetId.TrimAllSpacesAroundNullSafe();

            if (!ValidateRequest(out var badRequest,
                ParamsToValidate.UnsupportedAssetId | ParamsToValidate.EmptyClientId | ParamsToValidate.EmptyBlockchainType,
                blockchainType: blockchainType, assetId: assetId, clientId: clientId))
                return badRequest;

            if (blockchainType == SpecialBlockchainTypes.FirstGenerationBlockchain && !await _walletService.DoesAssetExistAsync(assetId))
            {
                return BadRequest
                (
                    BlockchainWalletsErrorResponse.Create($"Asset [{assetId}] does not exist.")
                );
            }

            if (await _walletService.DefaultWalletExistsAsync(blockchainType, assetId, clientId))
            {
                return StatusCode
                (
                    (int)HttpStatusCode.Conflict,
                    BlockchainWalletsErrorResponse.Create($"Wallet for specified client [{clientId}] has already been created.")
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
                IntegrationLayerAssetId = wallet.AssetId,
                CreatedBy = wallet.CreatorType
            });
        }

        /// <summary>
        ///    Removes wallet for the specified client in the specified blockchain type/asset pair
        /// </summary>
        [Obsolete]
        [HttpDelete(RouteSuffix + "/by-client-ids/{clientId}")]
        public async Task<IActionResult> DeleteWallet([FromRoute] string blockchainType, [FromRoute] string assetId, [FromRoute] Guid clientId)
        {
            blockchainType = blockchainType.TrimAllSpacesAroundNullSafe();
            assetId = assetId.TrimAllSpacesAroundNullSafe();

            if (!ValidateRequest(out var badRequest,
                ParamsToValidate.UnsupportedAssetId | ParamsToValidate.EmptyClientId | ParamsToValidate.EmptyBlockchainType,
                blockchainType: blockchainType, assetId: assetId, clientId: clientId))
                return badRequest;

            if (!await _walletService.WalletExistsAsync(blockchainType, assetId, clientId))
            {
                return NotFound
                (
                    BlockchainWalletsErrorResponse.Create($"Wallet for specified client [{clientId}] does not exist.")
                );
            }

            await _walletService.DeleteWalletsAsync(blockchainType, assetId, clientId);

            return Accepted();
        }

        /// <summary>
        ///    Returns wallet address for the specified client in the specified blockchain type/asset pair.
        /// </summary>
        [Obsolete]
        [HttpGet(RouteSuffix + "/by-client-ids/{clientId}/address")]
        public async Task<IActionResult> GetAddress([FromRoute] string blockchainType, [FromRoute] string assetId, [FromRoute] Guid clientId)
        {
            blockchainType = blockchainType.TrimAllSpacesAroundNullSafe();
            assetId = assetId.TrimAllSpacesAroundNullSafe();

            if (!ValidateRequest(out var badRequest,
                ParamsToValidate.EmptyBlockchainType | ParamsToValidate.EmptyAssetId | ParamsToValidate.EmptyClientId,
                blockchainType: blockchainType, assetId: assetId, clientId: clientId))
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
        [Obsolete]
        public async Task<IActionResult> GetClientId([FromRoute] string blockchainType, [FromRoute] string assetId, [FromRoute] string address)
        {
            blockchainType = blockchainType.TrimAllSpacesAroundNullSafe();
            address = address.TrimAllSpacesAroundNullSafe();

            if (!ValidateRequest(out var badRequest,
                ParamsToValidate.EmptyBlockchainType | ParamsToValidate.EmptyAddress,
                blockchainType: blockchainType,
                address: address))
                return badRequest;

            var clientId = await _walletService.TryGetClientIdAsync(blockchainType, address);

            if (clientId != null)
            {
                return Ok(new ClientIdResponse
                {
                    ClientId = clientId.Value
                });
            }

            return NoContent();
        }

        /// <summary>
        ///    Returns all wallets for the specified client.
        /// </summary>
        [Obsolete]
        [HttpGet("all/by-client-ids/{clientId}")]
        public async Task<IActionResult> GetWallets([FromRoute] Guid clientId, [FromQuery] int take, [FromQuery] string continuationToken)
        {
            if (take <= 0)
            {
                return BadRequest(BlockchainWalletsErrorResponse.Create($"{nameof(take)} must be integer greater than 0."));
            }

            if (!ValidateRequest(out var badRequest,
                ParamsToValidate.EmptyClientId,
                clientId: clientId))
                return badRequest;

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
                    IntegrationLayerAssetId = x.AssetId,
                    CreatedBy = x.CreatorType
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

        private bool ValidateRequest(out IActionResult badRequest, ParamsToValidate flags, Guid clientId = default(Guid), string blockchainType = null, string address = null, string assetId = null)
        {
            badRequest = null;
            var invalidInputParams = new List<string>();

            if ((flags & ParamsToValidate.EmptyBlockchainType) != 0)
            {
                if (string.IsNullOrWhiteSpace(blockchainType))
                    invalidInputParams.Add($"This parameter should have a value: {nameof(blockchainType)}");
            }

            if ((flags & ParamsToValidate.EmptyAddress) != 0)
            {
                if (string.IsNullOrWhiteSpace(address))
                    invalidInputParams.Add($"This parameter should have a value: {nameof(address)}");
            }

            if ((flags & ParamsToValidate.EmptyAssetId) != 0)
            {
                if (string.IsNullOrWhiteSpace(assetId))
                    invalidInputParams.Add($"This parameter should have a value: {nameof(assetId)}");
            }

            if ((flags & ParamsToValidate.EmptyClientId) != 0)
            {
                if (clientId == default(Guid))
                    invalidInputParams.Add($"This parameter should have a value: {nameof(clientId)}");
            }

            if ((flags & ParamsToValidate.UnsupportedBlockchainType) == ParamsToValidate.UnsupportedBlockchainType)
            {
                if (!string.IsNullOrWhiteSpace(blockchainType) &&
                    !_blockchainIntegrationService.BlockchainIsSupported(blockchainType))
                    invalidInputParams.Add($"Blockchain type [{blockchainType}] is not supported.");
            }

            if ((flags & ParamsToValidate.UnsupportedAssetId) == ParamsToValidate.UnsupportedAssetId)
            {
                if (!string.IsNullOrWhiteSpace(blockchainType) &&
                    !string.IsNullOrWhiteSpace(assetId) &&
                    !_blockchainIntegrationService.AssetIsSupportedAsync(blockchainType, assetId).Result)
                    invalidInputParams.Add($"Asset [{assetId}] or/and blockchain type [{blockchainType}] is not supported.");
            }

            // ---

            if (invalidInputParams.Any())
            {
                badRequest = BadRequest(BlockchainWalletsErrorResponse.Create($"One or more input parameters are invalid: [{string.Join(", ", invalidInputParams)}]."));
                return false;
            }

            return true;
        }
    }
}
