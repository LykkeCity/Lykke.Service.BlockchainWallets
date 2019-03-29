using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Contract.Models;
using Lykke.Service.BlockchainWallets.Contract.Models.BlackLists;
using Lykke.Service.BlockchainWallets.Core.DTOs.Validation;
using Lykke.Service.BlockchainWallets.Core.Services.Validation;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Service.BlockchainWallets.Controllers
{
    public class BlackListController : Controller
    {
        private static readonly char[] _charactersToTrim = new char[] { ' ', '\t' };
        private readonly IBlackListService _blackListService;

        public BlackListController(IBlackListService blackListService)
        {
            _blackListService = blackListService;
        }

        /// <summary>
        /// is address black listed
        /// </summary>
        /// <returns></returns>
        [HttpGet("{blockchainType}/{blockedAddress}/is-blocked")]
        public async Task<IActionResult> IsBlockedAsync([FromRoute][Required] string blockchainType, [FromRoute][Required] string blockedAddress)
        {
            var isBlocked = await _blackListService.IsBlockedAsync(blockchainType, Trim(blockedAddress));

            return Ok(new IsBlockedResponse()
            {
                IsBlocked = isBlocked
            });
        }

        /// <summary>
        /// is address black listed
        /// </summary>
        /// <returns></returns>
        [HttpGet("{blockchainType}/{blockedAddress}")]
        public async Task<IActionResult> GetBlackListAsync([FromRoute][Required] string blockchainType, [FromRoute][Required] string blockedAddress)
        {
            var model = await _blackListService.TryGetAsync(blockchainType, Trim(blockedAddress));

            if (model == null)
                return NoContent();

            return Ok(Map(model));
        }

        /// <summary>
        /// Take blocked addresses for specific blockchainType
        /// </summary>
        /// <returns></returns>
        [HttpGet("{blockchainType}")]
        public async Task<IActionResult> GetAllAsync([FromRoute][Required] string blockchainType, [FromQuery] int take, [FromQuery] string continuationToken)
        {
            if (take <= 0)
                return BadRequest
            (
                BlockchainWalletsErrorResponse.Create($"{nameof(take)} Field must greater than 0")
            );

            var (models, newToken) = await _blackListService.TryGetAllAsync(blockchainType, take, continuationToken);

            return Ok(new BlackListEnumerationResponse()
            {
                ContinuationToken = newToken,
                List = models.Select(x => Map(x))
            });
        }

        /// <summary>
        /// Add black listed address
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> AddBlockedAddressAsync([FromBody] BlackListRequest request)
        {
            string blockedAddress = Trim(request.BlockedAddress);

            BlackListModel model = new BlackListModel(request.BlockchainType, blockedAddress, request.IsCaseSensitive);

            await _blackListService.SaveAsync(model);

            return Ok();
        }

        /// <summary>
        /// Add black listed address
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        public async Task<IActionResult> UpdateBlockedAddressAsync([FromBody] BlackListRequest request)
        {
            string blockedAddress = Trim(request.BlockedAddress);

            BlackListModel model = new BlackListModel(request.BlockchainType, blockedAddress, request.IsCaseSensitive);

            await _blackListService.SaveAsync(model);

            return Ok();
        }

        /// <summary>
        /// Delete black listed address
        /// </summary>
        /// <returns></returns>
        [HttpDelete("{blockchainType}/{blockedAddress}")]
        public async Task<IActionResult> DeleteBlockedAddressAsync([FromRoute][Required] string blockchainType, [FromRoute][Required] string blockedAddress)
        {
            await _blackListService.DeleteAsync(blockchainType, Trim(blockedAddress));

            return Ok();
        }

        private string Trim(string address)
        {
            return address?.Trim(_charactersToTrim);
        }

        private BlackListResponse Map(BlackListModel blackListModel)
        {
            return new BlackListResponse()
            {
                IsCaseSensitive = blackListModel.IsCaseSensitive,
                BlockedAddress = blackListModel.BlockedAddress,
                BlockchainType = blackListModel.BlockchainType
            };
        }
    }
}
