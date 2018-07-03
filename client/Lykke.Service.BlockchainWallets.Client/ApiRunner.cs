using System;
using System.Net;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainWallets.Contract.Models;
using Polly;
using Refit;

namespace Lykke.Service.BlockchainWallets.Client
{
    internal class ApiRunner
    {
        private readonly int _defaultRetriesCount;

        public ApiRunner(int defaultRetriesCount = int.MaxValue)
        {
            _defaultRetriesCount = defaultRetriesCount;
        }
        
        public static async Task<T> RunAsync<T>(Func<Task<T>> method)
        {
            try
            {
                return await method();
            }
            catch (ApiException ex)
            {
                throw new ErrorResponseException(GetErrorResponse(ex), ex);
            }
        }

        public Task RunWithRetriesAsync(Func<Task> method, int? retriesCount = null)
        {
            // TODO: Update retries telemetry
            return Policy
                .Handle<Exception>(FilterRetryExceptions)
                .WaitAndRetryAsync(retriesCount ?? _defaultRetriesCount, GetRetryDelay)
                .ExecuteAsync(async () =>
                {
                    try
                    {
                        await method();
                    }
                    catch (ApiException ex)
                    {
                        throw new ErrorResponseException(GetErrorResponse(ex), ex);
                    }
                });
        }

        public Task<T> RunWithRetriesAsync<T>(Func<Task<T>> method, int? retriesCount = null)
        {
            // TODO: Update retries telemetry

            return Policy
                .Handle<Exception>(FilterRetryExceptions)
                .WaitAndRetryAsync(retriesCount ?? _defaultRetriesCount, GetRetryDelay)
                .ExecuteAsync(async () =>
                {
                    try
                    {
                        return await method();
                    }
                    catch (ApiException ex)
                    {
                        throw new ErrorResponseException(GetErrorResponse(ex), ex);
                    }
                });
        }

        private static bool FilterRetryExceptions(Exception ex)
        {
            if (ex.InnerException is ApiException innerApiException)
            {
                return innerApiException.StatusCode == HttpStatusCode.InternalServerError ||
                       innerApiException.StatusCode == HttpStatusCode.BadGateway ||
                       innerApiException.StatusCode == HttpStatusCode.ServiceUnavailable ||
                       innerApiException.StatusCode == HttpStatusCode.GatewayTimeout;
            }

            if (ex is ApiException apiException)
            {
                return apiException.StatusCode == HttpStatusCode.InternalServerError ||
                       apiException.StatusCode == HttpStatusCode.BadGateway ||
                       apiException.StatusCode == HttpStatusCode.ServiceUnavailable ||
                       apiException.StatusCode == HttpStatusCode.GatewayTimeout;
            }

            return true;
        }

        private static BlockchainWalletsErrorResponse GetErrorResponse(ApiException ex)
        {
            BlockchainWalletsErrorResponse errorResponse;

            try
            {
                errorResponse = ex.GetContentAs<BlockchainWalletsErrorResponse>();
            }
            catch (Exception)
            {
                try
                {
                    var errorResponseOldV = ex.GetContentAs<ErrorResponse>();
                    errorResponse = BlockchainWalletsErrorResponse.Create(errorResponseOldV.ErrorMessage,
                        ErrorType.None);
                }
                catch (Exception e)
                {
                    errorResponse = null;
                }
            }

            return errorResponse ?? 
                   BlockchainWalletsErrorResponse.Create("Blockchain API is not specify the error response", 
                       ErrorType.None);
        }

        private static TimeSpan GetRetryDelay(int retryAttempt)
        {
            if (retryAttempt < 3)
            {
                return TimeSpan.FromMilliseconds(500 * retryAttempt);
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement ... for better readibility
            if (retryAttempt < 8)
            {
                return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 2));
            }

            return TimeSpan.FromMinutes(1);
        }
    }
}
