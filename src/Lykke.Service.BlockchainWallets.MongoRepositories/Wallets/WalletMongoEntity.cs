using System;
using Lykke.Service.BlockchainWallets.Contract;
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

        [BsonElement("ins")]
        public DateTime Inserted { get; set; }

        [BsonElement("upd")]
        public DateTime Updated { get; set; }

        [BsonElement("vers")]
        public int Version { get; set; }

        public static WalletMongoEntity Create(
            ObjectId id,
            string blockchainType, 
            Guid clientId, 
            string address,
            CreatorTypeValues creatorType, 
            DateTime inserted,
            DateTime updated)
        {
            return new WalletMongoEntity
            {
                ClientId = clientId,
                Address = address,
                BlockchainType = blockchainType,
                Id = id,
                CreatorType = creatorType,
                Inserted = inserted,
                Updated = updated,
                Version = 0
            };
        }

        public enum CreatorTypeValues
        {
            LykkeWallet = 1,
            LykkePay = 2
        }
    }
}
