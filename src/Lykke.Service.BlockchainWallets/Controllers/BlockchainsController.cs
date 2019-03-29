using Common;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Contract.Models;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Enums;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Lykke.Service.BlockchainWallets.Controllers
{
    [Route("api/blockchains")]
    public class BlockchainsController : Controller
    {
        private static char[] _trimmedChars = new char[] { ' ', '\t' };
        private const string RouteSuffix = "{blockchainType}";

        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly IBlockchainAssetService _blockchainAssetService;
        private readonly IWalletService _walletService;

        public BlockchainsController(
            IBlockchainIntegrationService blockchainIntegrationService,
            IWalletService walletService,
            IBlockchainAssetService blockchainAssetService)
        {
            _walletService = walletService;
            _blockchainIntegrationService = blockchainIntegrationService;
            _blockchainAssetService = blockchainAssetService;
        }

        [HttpPost(RouteSuffix + "/clients/{clientId}/wallets")]
        public async Task<IActionResult> CreateWallet([FromRoute] string blockchainType, [FromRoute] Guid clientId,
            [FromQuery] CreatorType createdBy)
        {
            if (!ValidateRequest(blockchainType, clientId, out var badRequest))
            {
                return badRequest;
            }

            if (blockchainType == SpecialBlockchainTypes.FirstGenerationBlockchain)
            {
                return BadRequest
                (
                    BlockchainWalletsErrorResponse.Create($"{SpecialBlockchainTypes.FirstGenerationBlockchain} does not supported.")
                );
            }

            var wallet = await _walletService.CreateWalletAsync(blockchainType, clientId, createdBy);

            return Ok(new BlockchainWalletResponse
            {
                Address = wallet.Address,
                AddressExtension = wallet.AddressExtension,
                BaseAddress = wallet.BaseAddress,
                BlockchainType = wallet.BlockchainType,
                ClientId = wallet.ClientId,
                CreatedBy = wallet.CreatorType
            });
        }

        [HttpGet(RouteSuffix + "/clients/{clientId}/wallets")]
        public async Task<IActionResult> GetWallets([FromRoute] string blockchainType, [FromRoute] Guid clientId, [FromQuery] int take, [FromQuery] string continuationToken)
        {
            if (take <= 0)
            {
                return BadRequest(BlockchainWalletsErrorResponse.Create($"{nameof(take)} should be integer greater then 0."));
            }

            if (!ValidateRequest(blockchainType, clientId, out var badRequest))
            {
                return badRequest;
            }

            var (wallets, token) = await _walletService.GetClientWalletsAsync(blockchainType, clientId, take, continuationToken);

            var response = new BlockchainWalletsResponse
            {
                Wallets = wallets.Select(x => new BlockchainWalletResponse
                {
                    Address = x.Address,
                    AddressExtension = x.AddressExtension,
                    BaseAddress = x.BaseAddress,
                    BlockchainType = x.BlockchainType,
                    ClientId = x.ClientId,
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

        //api/blockchains/{blockchainType}/clients/{clientId}/wallets/{address}
        [HttpDelete(RouteSuffix + "clients/{clientId}/wallets/{address}")]
        public async Task<IActionResult> DeleteWallet([FromRoute] string blockchainType, [FromRoute] Guid clientId, [FromRoute] string address)
        {
            if (!ValidateRequest(blockchainType, clientId, out var badRequest))
            {
                return badRequest;
            }

            if (!await _walletService.WalletExistsAsync(blockchainType, clientId, address))
            {
                return NotFound
                (
                    BlockchainWalletsErrorResponse.Create($"Wallet for specified client [{clientId}] does not exist.")
                );
            }

            await _walletService.DeleteWalletsAsync(blockchainType, clientId, address);

            return Accepted();
        }

        [HttpGet(RouteSuffix + "/wallets/{address}")]
        public async Task<IActionResult> GetWallet([FromRoute] string blockchainType, [FromRoute] string address)
        {
            if (!ValidateRequest(blockchainType, address, out var badRequest))
            {
                return badRequest;
            }

            var wallet = await _walletService.TryGetWalletAsync(blockchainType, address);

            if (wallet == null)
                return NoContent();

            var response = new BlockchainWalletResponse
            {
                Address = wallet.Address,
                AddressExtension = wallet.AddressExtension,
                BaseAddress = wallet.BaseAddress,
                BlockchainType = wallet.BlockchainType,
                ClientId = wallet.ClientId,
                CreatedBy = wallet.CreatorType
            };

            return Ok(response);
        }

        [HttpGet(RouteSuffix + "/wallets/{address}/created-by")]
        public async Task<IActionResult> GetCreatedBy([FromRoute] string blockchainType, [FromRoute] string address)
        {
            if (!ValidateRequest(blockchainType, address, out var badRequest))
            {
                return badRequest;
            }

            var wallet = await _walletService.TryGetWalletAsync(blockchainType, address);

            if (wallet == null)
                return NoContent();

            var response = new CreatedByResponse
            {
               CreatedBy = wallet.CreatorType
            };

            return Ok(response);
        }


        /// <summary>
        ///    Return client id for the specified wallet.
        /// </summary>
        [HttpGet("/api/blockchains/{blockchainType}/wallets/{address}/client-id")]
        public async Task<IActionResult> GetClientId([FromRoute] string blockchainType, [FromRoute] string address)
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
            else
            {
                return NoContent();
            }
        }

        private bool ValidateRequest(string blockchainType, string address, out IActionResult badRequest)
        {
            var invalidInputParams = new List<string>();

            if (string.IsNullOrWhiteSpace(blockchainType) ||
                !_blockchainIntegrationService.BlockchainIsSupported(blockchainType))
            {
                invalidInputParams.Add($"Blockchain type [{blockchainType}] is empty or not supported.");
            }

            if (string.IsNullOrEmpty(address))
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
                BlockchainWalletsErrorResponse.Create($"One or more input parameters [{string.Join(", ", invalidInputParams)}] are invalid.")
            );

            return false;
        }

        private bool ValidateRequest(string blockchainType, Guid clientId, out IActionResult badRequest)
        {
            var invalidInputParams = new List<string>();

            if (string.IsNullOrWhiteSpace(blockchainType) ||
                !_blockchainIntegrationService.BlockchainIsSupported(blockchainType))
            {
                invalidInputParams.Add($"Blockchain type [{blockchainType}] is empty or not supported.");
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
                BlockchainWalletsErrorResponse.Create($"One or more input parameters [{string.Join(", ", invalidInputParams)}] are invalid.")
            );

            return false;
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
                    !_blockchainAssetService.IsAssetSupported(blockchainType, assetId))
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
