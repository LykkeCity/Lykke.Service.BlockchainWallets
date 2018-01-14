using System.Collections.Generic;
using Lykke.Service.BlockchainWallets.Core.Domain.Health;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IHealthService
    {
        string GetHealthViolationMessage();

        IEnumerable<HealthIssue> GetHealthIssues();
    }
}
