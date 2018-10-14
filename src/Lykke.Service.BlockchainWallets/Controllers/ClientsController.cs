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
    [Route("api/clients")]
    public class ClientsController : Controller
    {
        private static char[] _trimmedChars = new char[] { ' ', '\t' };

        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly IWalletService _walletService;

        public ClientsController(
            IBlockchainIntegrationService blockchainIntegrationService,
            IWalletService walletService)
        {
            _walletService = walletService;
            _blockchainIntegrationService = blockchainIntegrationService;
        }

        /// <summary>
        ///    Returns all wallets for the specified client.
        /// </summary>
        [HttpGet("{clientId}/actual-wallets")]
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

            var response = new BlockchainWalletsResponse
            {
                Wallets = wallets.Select(x => new BlockchainWalletResponse()
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
