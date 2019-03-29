using Lykke.Service.BlockchainWallets.Contract.Models;
using Lykke.Service.BlockchainWallets.Core.DTOs.Validation;
using Lykke.Service.BlockchainWallets.Core.Services.Validation;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using ValidationErrorType = Lykke.Service.BlockchainWallets.Contract.Models.ValidationErrorType;

namespace Lykke.Service.BlockchainCashoutPreconditionsCheck.Controllers
{
    [Route("/api/blockchains")]
    public class CashoutCheckController : Controller
    {
        private readonly IValidationService _validationService;

        public CashoutCheckController(IValidationService validationService)
        {
            _validationService = validationService;
        }

        [HttpGet("{blockchainType}/cashout-destinations/{address}/allowability")]
        public async Task<IActionResult> CheckCashoutDestinationAsync(
            [Required][FromQuery] string blockchainType,
            [Required][FromQuery] string address)
        {
            var cashoutModel = new CashoutModel()
            {
                BlockchainType = blockchainType,
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
