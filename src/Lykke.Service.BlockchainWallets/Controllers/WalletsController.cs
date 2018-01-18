﻿using System;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Models;
using Microsoft.AspNetCore.Mvc;


namespace Lykke.Service.BlockchainWallets.Controllers
{
    [Route("api/wallets/{integrationLayerId}/{integrationLayerAssetId}")]
    public class WalletsController : Controller
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly IWalletService                _walletService;


        public WalletsController(
            IBlockchainIntegrationService blockchainIntegrationService,
            IWalletService walletService)
        {
            _walletService     = walletService;
            _blockchainIntegrationService = blockchainIntegrationService;
        }


        [HttpPost("by-client-ids/{clientId}")]
        public async Task<IActionResult> CreateWallet([FromRoute] string integrationLayerId, [FromRoute] string integrationLayerAssetId, [FromRoute] Guid clientId)
        {
            if (!await _blockchainIntegrationService.AssetIsSupported(integrationLayerId, integrationLayerAssetId))
            {
                return NotFound();
            }
            
            if (await _walletService.WalletExistsAsync(integrationLayerId, integrationLayerAssetId, clientId))
            {
                return StatusCode((int) HttpStatusCode.Conflict);
            }

            var walletAddress = await _walletService.CreateWalletAsync(integrationLayerId, integrationLayerAssetId, clientId);

            return Ok(new WalletCreatedResponse
            {
                Address = walletAddress
            });
        }

        [HttpDelete("by-client-ids/{clientId}")]
        public async Task<IActionResult> DeleteWallet([FromRoute] string integrationLayerId, [FromRoute] string integrationLayerAssetId, [FromRoute] Guid clientId)
        {
            if (!await _blockchainIntegrationService.AssetIsSupported(integrationLayerId, integrationLayerAssetId))
            {
                return NotFound();
            }

            if (!await _walletService.WalletExistsAsync(integrationLayerId, integrationLayerAssetId, clientId))
            {
                return NotFound();
            }

            await _walletService.DeleteWalletAsync(integrationLayerId, integrationLayerAssetId, clientId);

            return Accepted();
        }

        [HttpGet("by-client-ids/{clientId}/address")]
        public async Task<IActionResult> GetAddress([FromRoute] string integrationLayerId, [FromRoute] string integrationLayerAssetId, [FromRoute] Guid clientId)
        {
            var address = await _walletService.GetAddressAsync(integrationLayerId, integrationLayerAssetId, clientId);

            if (string.IsNullOrEmpty(address))
            {
                return NotFound();
            }

            return Ok(new AddressResponse
            {
                Address = address
            });
        }

        [HttpGet("by-addresses/{address}/client-id")]
        public async Task<IActionResult> GetClientId([FromRoute] string integrationLayerId, [FromRoute] string integrationLayerAssetId, [FromRoute] string address)
        {
            var clientId = await _walletService.GetClientIdAsync(integrationLayerId, integrationLayerAssetId, address);

            if (!clientId.HasValue)
            {
                return NotFound();
            }

            return Ok(new ClientIdResponse
            {
                ClientId = clientId.Value
            });
        }
    }
}
