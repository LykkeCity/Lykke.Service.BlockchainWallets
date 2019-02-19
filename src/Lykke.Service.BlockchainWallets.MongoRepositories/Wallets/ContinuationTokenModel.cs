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
            return this.ToJson().ToBase64();
        }

        public static ContinuationTokenModel Deserialize(string source)
        {
            return source.Base64ToString().DeserializeJson<ContinuationTokenModel>();
        }
    }
}
