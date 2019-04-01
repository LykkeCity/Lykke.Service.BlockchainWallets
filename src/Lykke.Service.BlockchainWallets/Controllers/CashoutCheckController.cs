using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Contract.Models;
using Lykke.Service.BlockchainWallets.Core.DTOs.Validation;
using Lykke.Service.BlockchainWallets.Core.Services.Validation;
using Microsoft.AspNetCore.Mvc;
using ValidationErrorType = Lykke.Service.BlockchainWallets.Contract.Models.ValidationErrorType;

namespace Lykke.Service.BlockchainWallets.Controllers
{
    //GET /api/cashout-destinations/{address}/assets/{assetId}/allowability?client={clientId}&amount={amount}
    [Route("/api/cashout-destinations")]
    public class CashoutCheckController : Controller
    {
        private readonly IValidationService _validationService;

        public CashoutCheckController(IValidationService validationService)
        {
            _validationService = validationService;
        }

        [HttpGet("{address}/assets/{assetId}/allowability")]
        public async Task<IActionResult> CheckCashoutDestinationAllowabilityAsync(
            [Required][FromRoute] string address,
            [Required][FromRoute] string assetId, 
            [FromQuery] Guid clientId,
            [FromQuery] decimal amount)
        {
            var cashoutModel = new CashoutModel()
            {
                AssetId = assetId,
                Amount = amount,
                ClientId = clientId,
                DestinationAddress = address
            };

            var validationErrors = await _validationService.ValidateAsync(cashoutModel);

            var response = new CashoutValidityResult
            {
                IsAllowed = (validationErrors?.Count ?? 0) == 0,
                ValidationErrors = validationErrors?.Select(x => ValidationErrorResponse.Create((ValidationErrorType)x.Type, x.Value)),
            };

            return Ok(response);
        }
    }
}
