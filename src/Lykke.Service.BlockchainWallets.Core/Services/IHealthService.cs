using System.Collections.Generic;
using Lykke.Common.Health;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IHealthService
    {
        IEnumerable<HealthIssue> GetHealthIssues();

        string GetHealthViolationMessage();
    }
}
