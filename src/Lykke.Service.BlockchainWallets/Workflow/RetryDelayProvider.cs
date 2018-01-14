using System;
using JetBrains.Annotations;

namespace Lykke.Service.BlockchainWallets.Workflow
{
    [UsedImplicitly]
    public class RetryDelayProvider
    {
        public TimeSpan RetryDelay { get; }

        public RetryDelayProvider(TimeSpan retryDelay)
        {
            RetryDelay = retryDelay;
        }
    }
}
