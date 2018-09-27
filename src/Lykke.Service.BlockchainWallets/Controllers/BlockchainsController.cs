using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Contract.Models;
using Lykke.Service.BlockchainWallets.Core.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;


namespace Lykke.Service.BlockchainWallets.Controllers
{
    [Route("api/blockchains")]
    public class BlockchainsController : Controller
    {
        private static char[] _trimmedChars = new char[] { ' ', '\t' };
        private const string RouteSuffix = "{blockchainType}";

        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly IWalletService _walletService;

        public BlockchainsController(
            IBlockchainIntegrationService blockchainIntegrationService,
            IWalletService walletService)
        {
            _walletService = walletService;
            _blockchainIntegrationService = blockchainIntegrationService;
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

        [HttpGet(RouteSuffix + "/clients/{clientId}/wallets")]
        public async Task<IActionResult> GetWallets([FromRoute] string blockchainType, [FromRoute] Guid clientId, [FromQuery] int take, [FromQuery] string continuationToken)
        {
            if (take <= 0)
            {
                return BadRequest(BlockchainWalletsErrorResponse.Create($"{nameof(take)} should be greater then 0."));
            }

            if (!ValidateRequest(blockchainType, clientId, out var badRequest))
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

            var response = new WalletResponse
            {
                Address = wallet.Address,
                AddressExtension = wallet.AddressExtension,
                BaseAddress = wallet.BaseAddress,
                BlockchainType = wallet.BlockchainType,
                ClientId = wallet.ClientId,
                IntegrationLayerId = wallet.BlockchainType,
                IntegrationLayerAssetId = wallet.AssetId
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


        private bool ValidateRequest(string blockchainType, string address, out IActionResult badRequest)
        {
            var invalidInputParams = new List<string>();

            if (string.IsNullOrWhiteSpace(blockchainType))
            {
                invalidInputParams.Add(nameof(blockchainType));
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

            if (string.IsNullOrWhiteSpace(blockchainType))
            {
                invalidInputParams.Add(nameof(blockchainType));
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
    }
}
