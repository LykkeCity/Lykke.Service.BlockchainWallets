using System;

namespace Lykke.Service.BlockchainWallets.MongoRepositories.Mongo.Query
{
    public class QueryOptions
    {
        public TimeSpan Timeout { get; set; }

        public int RetryCount { get; set; }

        public static QueryOptions Default()
        {
            return new QueryOptions
            {
                Timeout = TimeSpan.FromSeconds(5),
                RetryCount = 5
            };
        }
    }
}
