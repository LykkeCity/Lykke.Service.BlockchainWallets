using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.BlockchainWallets.MongoRepositories.Wallets.Entities
{
    internal interface IWallet
    {
        Guid ClientId { get; }
        
        string BlockchainType { get; }
        
        string Address { get; }

        CreatorType CreatorType { get; }

        DateTime Inserted { get; }

        DateTime Updated { get; }
    }
}
