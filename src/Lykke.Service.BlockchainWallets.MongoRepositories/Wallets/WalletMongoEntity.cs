using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Lykke.Service.BlockchainWallets.MongoRepositories.Wallets
{
    public class WalletMongoEntity
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("clid")]
        public Guid ClientId { get; set; }

        [BsonElement("btyp")]
        public string BlockchainType { get; set; }

        [BsonElement("addr")]
        public string Address { get; set; }

        [BsonElement("crtr")]
        public CreatorTypeValues CreatorType { get; set; }

        [BsonElement("prmr")]
        public bool IsPrimary { get; set; }

        [BsonElement("ins")]
        public DateTime Inserted { get; set; }

        [BsonElement("upd")]
        public DateTime Updated { get; set; }

        public enum CreatorTypeValues
        {
            LykkeWallet = 1,
            LykkePay = 2
        }
    }
}
