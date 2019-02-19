using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using MongoDB.Driver;
using Polly;

namespace Lykke.Service.BlockchainWallets.MongoRepositories.Mongo.Command
{
    public static class CommandExtensions
    {
        public static Task WrapCommandAsync(Func<Task> command, ILog log, CommandOptions options = null)
        {
            options = options ?? CommandOptions.Default();

            return Policy.Handle<Exception>(NeedToRetry)
                .RetryAsync(options.RetryCount, onRetry: (ex, retryNumber, context) =>
                {
                    log.Warning("Retrying command", ex);
                }).ExecuteAsync(command);
        }

        private static bool NeedToRetry(Exception e)
        {
            return e is MongoExecutionTimeoutException
                   || e is MongoConnectionException;
        }
    }
}
