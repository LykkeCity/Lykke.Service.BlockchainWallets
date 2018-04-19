using System.Collections.Generic;
using JetBrains.Annotations;
using Lykke.Common.Health;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Core.Settings.BlockchainIntegrationSettings;

namespace Lykke.Service.BlockchainWallets.Services
{
    [UsedImplicitly]
    public class HealthService : IHealthService
    {
        private readonly BlockchainsIntegrationSettings _settings;


        public HealthService(
            BlockchainsIntegrationSettings settings)
        {
            _settings = settings;
        }


        public string GetHealthViolationMessage()
        {
            return null;
        }

        public IEnumerable<HealthIssue> GetHealthIssues()
        {
            var issues = new HealthIssuesCollection();

            foreach (var blockchain in _settings.Blockchains)
            {
                issues.Add($"{blockchain.Type} - API", blockchain.ApiUrl);
                issues.Add($"{blockchain.Type} - Hot Wallet", blockchain.HotWalletAddress);
            }

            return issues;
        }
    }
}
