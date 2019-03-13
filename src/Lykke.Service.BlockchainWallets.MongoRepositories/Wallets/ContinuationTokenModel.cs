using System;
using System.Collections.Generic;
using System.Text;
using Common;

namespace Lykke.Service.BlockchainWallets.MongoRepositories.Wallets
{
    public class ContinuationTokenModel
    {
        public int Skip { get; set; }

        public string Serialize()
        {
            return Skip.ToString();
        }

        public static ContinuationTokenModel Deserialize(string source)
        {
            return new ContinuationTokenModel
            {
                Skip = int.Parse(source)
            };
        }
    }
}
