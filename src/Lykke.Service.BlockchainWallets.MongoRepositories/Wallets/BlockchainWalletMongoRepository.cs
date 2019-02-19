using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.MongoRepositories.Mongo.Command;
using Lykke.Service.BlockchainWallets.MongoRepositories.Mongo.Query;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Lykke.Service.BlockchainWallets.MongoRepositories.Wallets
{
    public class BlockchainWalletMongoRepository:IBlockchainWalletsRepository
    {
        private readonly IMongoCollection<WalletMongoEntity> _collection;
        private readonly ILog _log;
        
        private BlockchainWalletMongoRepository(IMongoCollection<WalletMongoEntity> collection,  
            ILogFactory logFactory)
        {
            _collection = collection;
            _log = logFactory.CreateLog(this);
        }

        public static IBlockchainWalletsRepository Create(string connectionString, string dbName, ILogFactory logFactory)
        {
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase(dbName);
            
            return new BlockchainWalletMongoRepository(db.GetCollection<WalletMongoEntity>("blockchain-wallets"),
                logFactory);
        }

        public async Task AddAsync(string blockchainType, Guid clientId, string address, CreatorType createdBy,
            string clientLatestDepositIndexManualPartitionKey = null, bool addAsLatest = true)
        {
            var now = DateTime.UtcNow;

            var existedId = (await _collection.WrapQueryAsync(_log,
                query => query.Where(p => p.ClientId == clientId && p.Address == address && p.BlockchainType == blockchainType).Select(p=>p.Id).Take(1)))
                .SingleOrDefault();

            var entityId = existedId != ObjectId.Empty ? existedId : ObjectId.GenerateNewId(now);
            
            await CommandExtensions.WrapCommandAsync(() => _collection.ReplaceOneAsync(Builders<WalletMongoEntity>.Filter.Eq(p => p.Id, entityId),
                new WalletMongoEntity
                {
                    ClientId = clientId,
                    Address = address,
                    BlockchainType = blockchainType,
                    CreatorType = createdBy.ToDomain(),
                    IsPrimary = addAsLatest,
                    Id = entityId,
                    Inserted = now,
                    Updated = now
                }, new UpdateOptions { IsUpsert = true }), _log);

            if (addAsLatest)
            {
                var primarywalletsIds = (await _collection.WrapQueryAsync(_log,
                    query => query
                        .Where(p => p.ClientId == clientId && p.IsPrimary && p.BlockchainType == blockchainType)
                        .Select(p => p.Id)))
                    .Where(p => p != entityId).ToList();

                if (primarywalletsIds.Any())
                {
                    var bulkOps = new List<WriteModel<WalletMongoEntity>>();

                    foreach (var id in primarywalletsIds)
                    {
                        var updateOneOp = new UpdateOneModel<WalletMongoEntity>(
                            Builders<WalletMongoEntity>.Filter.Eq(p => p.Id, id),
                            Builders<WalletMongoEntity>.Update.Set(p => p.IsPrimary, false).Set(p => p.Updated, DateTime.UtcNow));

                        bulkOps.Add(updateOneOp);
                    }

                    await CommandExtensions.WrapCommandAsync(async () =>
                    {
                        await _collection.BulkWriteAsync(bulkOps);
                    }, _log);
                }
            }
        }

        public async Task DeleteIfExistsAsync(string blockchainType, Guid clientId, string address)
        {
            var entityToDelete =(await _collection.WrapQueryAsync(_log,
                query => query.Where(p =>
                    p.Address == address && p.ClientId == clientId && p.BlockchainType == blockchainType)))
                .SingleOrDefault();

            if (entityToDelete?.IsPrimary ?? false)
            {
                var nextWallet = (await _collection.WrapQueryAsync(_log,
                    query => query.Where(p => p.ClientId == clientId && p.BlockchainType == blockchainType)
                        .OrderByDescending(p => p.Inserted).Take(1))).SingleOrDefault();

                if (nextWallet != null)
                {
                    await CommandExtensions.WrapCommandAsync(async () =>
                    {
                        await _collection.UpdateOneAsync(
                            Builders<WalletMongoEntity>.Filter.Eq(p => p.Id, nextWallet.Id),
                            Builders<WalletMongoEntity>.Update.Set(p => p.IsPrimary, true));
                    }, _log);
                }
            }
        }

        public async Task<bool> ExistsAsync(string blockchainType, string address)
        {
            return (await TryGetAsync(blockchainType, address)) != null;
        }

        public async Task<bool> ExistsAsync(string blockchainType, Guid clientId)
        {
            return (await TryGetAsync(blockchainType, clientId)) != null;
        }

        public Task<(IEnumerable<WalletDto> Wallets, string ContinuationToken)> GetAllAsync(string blockchainType, 
            Guid clientId,
            int take,
            string continuationToken = null)
        {
            return GetDataWithContinuationTokenAsync(query => 
                query.Where(p => p.ClientId == clientId && p.BlockchainType == blockchainType), take, continuationToken);
        }

        public Task<(IEnumerable<WalletDto> Wallets, string ContinuationToken)> GetAllAsync(Guid clientId, 
            int take, 
            string continuationToken = null)
        {
            return GetDataWithContinuationTokenAsync(query =>
                query.Where(p => p.ClientId == clientId && p.IsPrimary), take, continuationToken);
        }

        private async Task<(IEnumerable<WalletDto> Wallets, string ContinuationToken)> GetDataWithContinuationTokenAsync(
                Func<IMongoQueryable<WalletMongoEntity>, IMongoQueryable<WalletMongoEntity>> queryBuilder, 
                int take, 
                string continuationToken)
        {
            var skip = 0;
            if (!string.IsNullOrEmpty(continuationToken))
            {
                skip = ContinuationTokenModel.Deserialize(continuationToken).Skip;
            }

            var entities = (await _collection.WrapQueryAsync(_log, query => queryBuilder(query).Skip(skip).Take(take))).ToList();

            var resultedContinuationToken = entities.Count < take
                ? null
                : new ContinuationTokenModel
                {
                    Skip = skip + take
                }.Serialize();
            
            return (entities.Select(ConvertEntityToDto), resultedContinuationToken);
        }

        public async Task<WalletDto> TryGetAsync(string blockchainType, string address)
        {
            var queryResult = (await _collection.WrapQueryAsync(_log,
                    query => query.Where(p => p.Address == address && p.BlockchainType == blockchainType)))
                .SingleOrDefault();
            
            if (queryResult != null)
            {
                return ConvertEntityToDto(queryResult);
            }

            return null;
        }

        public async Task<WalletDto> TryGetAsync(string blockchainType, Guid clientId)
        {
            var queryResult = (await _collection.WrapQueryAsync(_log,
                    query => query.Where(p => p.ClientId == clientId && p.IsPrimary && p.BlockchainType == blockchainType)))
                .SingleOrDefault();

            if (queryResult != null)
            {
                return ConvertEntityToDto(queryResult);
            }

            return null;
        }

        private static WalletDto ConvertEntityToDto(WalletMongoEntity entity)
        {
            return new WalletDto
            {
                Address = entity.Address,
                BlockchainType = entity.BlockchainType,
                ClientId = entity.ClientId,
                CreatorType = entity.CreatorType.FromDomain()
            };
        }

        #region Indexes

        public async Task EnsureIndexesCreatedAsync()
        {
            await CreateSupportClientIdPrimaryWalletsQueryAsync();
            await CreateSupportAddressQueryAsync();
            await CreateConsistencyClientAddressIndex();
        }

        private async Task CreateSupportClientIdPrimaryWalletsQueryAsync()
        {
            var clientIdAsc = Builders<WalletMongoEntity>.IndexKeys.Ascending(p => p.ClientId);
            var isPrimaryDesc = Builders<WalletMongoEntity>.IndexKeys.Descending(p => p.IsPrimary);
            var blockchainTypeAsc = Builders<WalletMongoEntity>.IndexKeys.Ascending(p => p.BlockchainType);

            var combined = Builders<WalletMongoEntity>.IndexKeys.Combine(clientIdAsc, isPrimaryDesc, blockchainTypeAsc);

            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<WalletMongoEntity>(combined,
                new CreateIndexOptions { Background = true}));
        }

        private async Task CreateSupportAddressQueryAsync()
        {
            var addressAsc = Builders<WalletMongoEntity>.IndexKeys.Ascending(p => p.Address);
            var blockchainTypeAsc = Builders<WalletMongoEntity>.IndexKeys.Ascending(p => p.BlockchainType);

            var combined = Builders<WalletMongoEntity>.IndexKeys.Combine(addressAsc, blockchainTypeAsc);

            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<WalletMongoEntity>(combined,
                new CreateIndexOptions { Background = true }));
        }

        private async Task CreateConsistencyClientAddressIndex()
        {
            var clientAsc = Builders<WalletMongoEntity>.IndexKeys.Ascending(p => p.ClientId);
            var addressAsc = Builders<WalletMongoEntity>.IndexKeys.Ascending(p => p.Address);
            var blockchainTypeAsc = Builders<WalletMongoEntity>.IndexKeys.Ascending(p => p.BlockchainType);

            var combined = Builders<WalletMongoEntity>.IndexKeys.Combine(clientAsc, addressAsc, blockchainTypeAsc);

            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<WalletMongoEntity>(combined,
                new CreateIndexOptions { Background = true, Unique = true}));
        }

        #endregion
    }
}
