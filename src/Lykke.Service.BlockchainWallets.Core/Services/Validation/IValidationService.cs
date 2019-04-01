using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.DTOs.Validation;

namespace Lykke.Service.BlockchainWallets.Core.Services.Validation
{
    public interface IValidationService
    {
        Task<IReadOnlyCollection<ValidationError>> ValidateAsync(CashoutModel cashoutModel);
    }
}
