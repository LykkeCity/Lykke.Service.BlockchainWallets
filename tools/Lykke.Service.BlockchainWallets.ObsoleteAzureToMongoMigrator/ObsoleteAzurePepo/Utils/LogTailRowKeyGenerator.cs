using System;

namespace Lykke.Service.BlockchainWallets.ObsoleteAzureToMongoMigrator.ObsoleteAzurePepo.Utils
{
    /// <summary>
    /// Log tail pattern:
    /// Retrieve the n entities most recently added to a partition by using a RowKey value that 
    /// sorts in reverse date and time order.
    /// </summary>
    public static class LogTailRowKeyGenerator
    {
        public static string GenerateRowKey()
        {
            return GenerateRowKey(DateTime.UtcNow);
        }

        public static string GenerateRowKey(DateTime dt)
        {
            string invertedTicks = string.Format("{0:D19}", DateTime.MaxValue.Ticks - dt.Ticks);

            return invertedTicks;
        }
    }
}
