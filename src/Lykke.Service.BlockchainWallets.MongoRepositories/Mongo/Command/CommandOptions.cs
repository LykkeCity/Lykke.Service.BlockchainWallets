namespace Lykke.Service.BlockchainWallets.MongoRepositories.Mongo.Command
{
    public class CommandOptions
    {
        public int RetryCount { get; set; }

        public static CommandOptions Default()
        {
            return new CommandOptions
            {
                RetryCount = 3
            };
        }
    }
}
