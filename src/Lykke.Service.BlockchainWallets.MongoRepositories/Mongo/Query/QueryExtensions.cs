using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Polly;

namespace Lykke.Service.BlockchainWallets.MongoRepositories.Mongo.Query
{
    public static class QueryExtensions
    {
        public static async Task<IEnumerable<TResult>> WrapQueryAsync<TEntity, TResult>(
            this IMongoCollection<TEntity> collection,
            ILog log,
            Func<IMongoQueryable<TEntity>, IMongoQueryable<TResult>> queryBuilder,
            QueryOptions queryOptions = null)
        {
            queryOptions = queryOptions ?? QueryOptions.Default();


            return await Policy.Handle<Exception>(NeedToRetry)
                .RetryAsync(queryOptions.RetryCount, onRetry: (ex, retryNumber, context) =>
                {
                    log.Warning("Retrying query", ex);
                })
                .ExecuteAsync(async () =>
                {
                    var query = queryBuilder(collection
                        .AsQueryable(aggregateOptions: new AggregateOptions
                        {
                            MaxTime = queryOptions.Timeout
                        }));

                    return await query.ToListAsync();
                });
        }


        private static bool NeedToRetry(Exception e)
        {
            return e is MongoExecutionTimeoutException 
                   || e is MongoConnectionException;
        }
    }
}
