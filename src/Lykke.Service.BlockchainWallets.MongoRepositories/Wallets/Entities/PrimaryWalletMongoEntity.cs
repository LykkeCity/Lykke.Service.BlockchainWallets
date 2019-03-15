using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Lykke.Service.BlockchainWallets.MongoRepositories.Wallets.Entities
{
    internal class PrimaryWalletMongoEntity: IWallet
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
        public CreatorType CreatorType { get; set; }

        [BsonElement("ins")]
        public DateTime Inserted { get; set; }

        [BsonElement("upd")]
        public DateTime Updated { get; set; }

        [BsonElement("vers")]
        public int Version { get; set; }

        public static PrimaryWalletMongoEntity Create(
            ObjectId id,
            string blockchainType, 
            Guid clientId, 
            string address,
            CreatorType creatorType, 
            DateTime inserted,
            DateTime updated)
        {
            return new PrimaryWalletMongoEntity
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
    }
}
