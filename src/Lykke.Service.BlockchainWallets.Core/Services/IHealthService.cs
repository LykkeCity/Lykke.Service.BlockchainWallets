using System.Collections.Generic;
using Lykke.Service.BlockchainWallets.Core.Domain.Health;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IHealthService
    {
        IEnumerable<HealthIssue> GetHealthIssues();
        string GetHealthViolationMessage();
    }
}
