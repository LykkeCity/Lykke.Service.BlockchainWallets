using Lykke.Service.BlockchainWallets.Contract.Models;
using Lykke.Service.BlockchainWallets.Contract.Models.BlackLists;
using Lykke.Service.BlockchainWallets.Core.DTOs.Validation;
using Lykke.Service.BlockchainWallets.Core.Services.Validation;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Controllers
{
    [Route("api/blockchains/{blockchainType}/black-addresses")]
    public class BlackListController : Controller
    {
        private static readonly char[] CharactersToTrim = new char[] { ' ', '\t' };
        private readonly IBlackListService _blackListService;

        public BlackListController(IBlackListService blackListService)
        {
            _blackListService = blackListService;
        }

        /// <summary>
        /// is address black listed
        /// </summary>
        /// <returns></returns>
        [HttpGet("{address}/is-blocked")]
        public async Task<IActionResult> IsBlockedAsync(
            [FromRoute][Required] string blockchainType, 
            [FromRoute][Required] string address)
        {
            var isBlocked = await _blackListService.IsBlockedAsync(blockchainType, Trim(address));

            return Ok(new IsBlockedResponse()
            {
                IsBlocked = isBlocked
            });
        }

        /// <summary>
        /// is address black listed
        /// </summary>
        /// <returns></returns>
        [HttpGet("{address}")]
        public async Task<IActionResult> GetBlackListAsync(
            [FromRoute][Required] string blockchainType, 
            [FromRoute][Required] string address)
        {
            var model = await _blackListService.TryGetAsync(blockchainType, Trim(address));

            if (model == null)
                return NoContent();

            return Ok(Map(model));
        }

        /// <summary>
        /// Take blocked addresses for specific blockchainType
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetAllAsync(
            [FromRoute][Required] string blockchainType, 
            [FromQuery] int take, 
            [FromQuery] string continuationToken)
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
        [HttpPost("{address}")]
        public async Task<IActionResult> AddBlockedAddressAsync(BlackListRequest request)
        {
            string blockedAddress = Trim(request.Address);

            BlackListModel model = new BlackListModel(request.BlockchainType, blockedAddress, request.IsCaseSensitive);

            await _blackListService.SaveAsync(model);

            return Ok();
        }

        /// <summary>
        /// Add black listed address
        /// </summary>
        /// <returns></returns>
        [HttpPut("{address}")]
        public async Task<IActionResult> UpdateBlockedAddressAsync(BlackListRequest request)
        {
            string blockedAddress = Trim(request.Address);

            BlackListModel model = new BlackListModel(request.BlockchainType, blockedAddress, request.IsCaseSensitive);

            await _blackListService.SaveAsync(model);

            return Ok();
        }

        /// <summary>
        /// Delete black listed address
        /// </summary>
        /// <returns></returns>
        [HttpDelete("{address}")]
        public async Task<IActionResult> DeleteBlockedAddressAsync(
            [FromRoute][Required] string blockchainType, 
            [FromRoute][Required] string address)
        {
            await _blackListService.DeleteAsync(blockchainType, Trim(address));

            return Ok();
        }

        private string Trim(string address)
        {
            return address?.Trim(CharactersToTrim);
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
