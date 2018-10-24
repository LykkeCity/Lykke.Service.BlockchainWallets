using System;
using System.Threading.Tasks;
using Polly;
using Polly.Timeout;

namespace Lykke.Service.BlockchainWallets.Client
{
    public class BlockchainWalletsFailureHandler: IBlockchainWalletsFailureHandler
    {
        private readonly Policy _circuitBreakPolicy;

        public BlockchainWalletsFailureHandler(TimeSpan durationOfBreak)
        {
            _circuitBreakPolicy = BuildCircuitBreakerPolicy(durationOfBreak);
        }

        public async Task<T> Execute<T>(Func<Task<T>> method, TimeSpan? timeout = null, Func<T> fallbackResult = null)
        {
            var fallbackPolicy = BuildFallbackPolicy(fallbackResult);
            var timeoutPolicy = BuildTimeoutPolicy(timeout);

            var pipeLine = fallbackPolicy.WrapAsync(timeoutPolicy.WrapAsync(_circuitBreakPolicy));

            return await pipeLine.ExecuteAsync(async () => await method());
        }

        private static Policy BuildTimeoutPolicy(TimeSpan? timeout)
        {
            if (timeout != null)
            {
                return Policy.TimeoutAsync(timeout.Value, TimeoutStrategy.Pessimistic);
            }
            else
            {
                return Policy.NoOpAsync();
            }
        }

        private static Policy<T> BuildFallbackPolicy<T>(Func<T> fallbackResult)
        {
            if (fallbackResult != null)
            {
                return Policy<T>
                    .Handle<Exception>()
                    .FallbackAsync(fallbackResult());
            }

            return Policy.NoOpAsync<T>();
        }

        private Policy BuildCircuitBreakerPolicy(TimeSpan durationOfBreak)
        {
            return Policy.Handle<Exception>(FilterCircuitBreakerExceptions)
                .CircuitBreakerAsync(exceptionsAllowedBeforeBreaking: 1,
                    durationOfBreak: durationOfBreak);
        }

        private bool FilterCircuitBreakerExceptions(Exception ex)
        {
            return true;
        }
    }
}
