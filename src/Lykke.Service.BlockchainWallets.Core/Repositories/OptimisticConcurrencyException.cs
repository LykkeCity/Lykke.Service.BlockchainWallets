using System;

namespace Lykke.Service.BlockchainWallets.Core.Repositories
{
    public class OptimisticConcurrencyException:Exception
    {
        public OptimisticConcurrencyException(Exception inner = null) : base("Concurrency ex", inner)
        {

        }
    }
}
