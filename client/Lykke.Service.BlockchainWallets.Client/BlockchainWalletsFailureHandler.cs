using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using MoreLinq;
using Polly;
using Polly.Timeout;

namespace Lykke.Service.BlockchainWallets.Client
{
    public class BlockchainWalletsFailureHandler: IBlockchainWalletsFailureHandler
    {
        private readonly Policy _circuitBreakPolicy;
        private readonly ISet<HttpStatusCode> _statusCodesToBreakCircuit;

        public BlockchainWalletsFailureHandler(TimeSpan durationOfBreak)
        {
            _circuitBreakPolicy = BuildCircuitBreakerPolicy(durationOfBreak);

            _statusCodesToBreakCircuit = new[]
            {
                HttpStatusCode.InternalServerError,
                HttpStatusCode.BadGateway,
                HttpStatusCode.GatewayTimeout,
                HttpStatusCode.ServiceUnavailable,
                HttpStatusCode.RequestTimeout
            }.ToHashSet();
        }

        public async Task<T> Execute<T>(Func<Task<T>> method, TimeSpan? timeout = null, Func<T> fallbackResult = null)
        {
            var fallbackPolicy = BuildFallbackPolicy(fallbackResult);
            var timeoutPolicy = BuildTimeoutPolicy(timeout);

            var pipeLine = fallbackPolicy.WrapAsync(_circuitBreakPolicy.WrapAsync(timeoutPolicy));

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
            return Policy.Handle<Exception>(NeedToBreakCircuit)
                .CircuitBreakerAsync(exceptionsAllowedBeforeBreaking: 1,
                    durationOfBreak: durationOfBreak);
        }

        private bool NeedToBreakCircuit(Exception ex)
        {
            if (ex is TimeoutRejectedException)
            {
                return true;
            }

            if (ex is ErrorResponseException errorResponceEx 
                && _statusCodesToBreakCircuit.Contains(errorResponceEx.StatusCode))
            {
                return true;
            }

            return false;
        }
    }
}
