using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.MongoRepositories.Mongo.Command;
using Lykke.Service.BlockchainWallets.MongoRepositories.Mongo.Query;
using Lykke.Service.BlockchainWallets.MongoRepositories.Wallets.Entities;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using CreatorType = Lykke.Service.BlockchainWallets.Contract.CreatorType;

namespace Lykke.Service.BlockchainWallets.MongoRepositories.Wallets
{
    public class BlockchainWalletMongoRepository:IBlockchainWalletsRepository
    {
        private readonly IMongoCollection<WalletMongoEntity> _allWalletsCollection;
        private readonly IMongoCollection<PrimaryWalletMongoEntity> _primaryWalletsCollection;
        private readonly ILog _log;
        private const int DuplicateUniqueIndexErrorCode = 11000;

        private BlockchainWalletMongoRepository(IMongoCollection<WalletMongoEntity> allWalletsCollection,
            IMongoCollection<PrimaryWalletMongoEntity> primaryWalletsCollection,
            ILogFactory logFactory)
        {
            _allWalletsCollection = allWalletsCollection;
            _primaryWalletsCollection = primaryWalletsCollection;
            _log = logFactory.CreateLog(this);
        }

        public static BlockchainWalletMongoRepository Create(string connectionString, string dbName, ILogFactory logFactory)
        {
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase(dbName);
            
            return new BlockchainWalletMongoRepository(db.GetCollection<WalletMongoEntity>("blockchain-wallets"), 
                db.GetCollection<PrimaryWalletMongoEntity>("primary-blockchain-wallets"),
                logFactory);
        }
        
        public async Task InsertBatchAsync(IEnumerable<(string blockchainType, Guid clientId, string address, CreatorType createdBy, bool isPrimary)> wallets)
        {
            var now = DateTime.UtcNow;

            var enumerated = wallets as IReadOnlyCollection<(string blockchainType, Guid clientId, string address, CreatorType createdBy, bool isPrimary)> ?? wallets.ToList();

            var insGeneral =  _allWalletsCollection.InsertManyAsync(enumerated.Select(w =>
                WalletMongoEntity.Create(ObjectId.GenerateNewId(now), 
                    blockchainType: w.blockchainType,
                    clientId: w.clientId, 
                    address: w.address, 
                    creatorType: w.createdBy.ToDomain(),
                    inserted: now,
                    updated: now)));

            var insPrimary = _primaryWalletsCollection.InsertManyAsync(enumerated
                .Where(p => p.isPrimary)
                .Select(w => PrimaryWalletMongoEntity.Create(ObjectId.GenerateNewId(now), 
                    blockchainType: w.blockchainType,
                    clientId: w.clientId,
                    address: w.address, 
                    creatorType: w.createdBy.ToDomain(),
                    inserted: now, 
                    updated: now)));

            await Task.WhenAll(insGeneral, insPrimary);
        }

        public async Task AddAsync(string blockchainType, Guid clientId, string address, CreatorType createdBy,
            string clientLatestDepositIndexManualPartitionKey = null, bool isPrimary = true)
        {
            var wallet = await InsertIfNotExistWalletAsync(blockchainType, clientId, address, createdBy.ToDomain());

            if (isPrimary)
            {
                await MakePrimaryAsync(wallet);
            }
        }

        private async Task<WalletMongoEntity> InsertIfNotExistWalletAsync(string blockchainType, 
            Guid clientId, 
            string address,
            Entities.CreatorType creatorType)
        {
            var now = DateTime.UtcNow;

            var entity = WalletMongoEntity.Create(
                id: ObjectId.GenerateNewId(DateTime.UtcNow),
                blockchainType: blockchainType,
                clientId: clientId,
                address: address,
                creatorType: creatorType,
                inserted: now,
                updated: now);

            await CommandExtensions.WrapCommandAsync(async () =>
            {
                try
                {
                    await _allWalletsCollection.InsertOneAsync(entity);
                }
                catch (MongoWriteException e) when(e.WriteError.Code == DuplicateUniqueIndexErrorCode)
                {
                    _log.Warning("Wallet already exist. Do nothing",
                        context: new
                        {
                            blockchainType,
                            clientId,
                            address,
                            creatorType
                        });
                }
            }, _log);

            return entity;
        }

        private async Task MakePrimaryAsync(WalletMongoEntity wallet, PrimaryWalletMongoEntity lastKnown = null)
        {
            await CommandExtensions.WrapCommandAsync(async () =>
            {
                var res = await _allWalletsCollection.UpdateOneAsync(p => p.Id == wallet.Id && p.Version == wallet.Version,
                    Builders<WalletMongoEntity>.Update.Inc(p => p.Version, 1));

                if (res.ModifiedCount != 1)
                {
                    throw new OptimisticConcurrencyException();
                }
            }, _log);

            if (lastKnown == null)
            {
                lastKnown = (await _primaryWalletsCollection.WrapQueryAsync(_log,
                        query => query.Where(p => p.ClientId == wallet.ClientId && p.BlockchainType == wallet.BlockchainType)))
                    .SingleOrDefault();
            }

            var now = DateTime.UtcNow;
            
            if (lastKnown == null)
            {
                var entity = PrimaryWalletMongoEntity.Create(
                    id: ObjectId.GenerateNewId(now),
                    blockchainType: wallet.BlockchainType,
                    clientId: wallet.ClientId,
                    address: wallet.Address,
                    creatorType: wallet.CreatorType,
                    inserted: now,
                    updated: now);
            
                await CommandExtensions.WrapCommandAsync(async () =>
                {
                    try
                    {
                        await _primaryWalletsCollection.InsertOneAsync(entity);
                    }
                    catch (MongoWriteException e) when (e.WriteError.Code == DuplicateUniqueIndexErrorCode)
                    {
                        throw new OptimisticConcurrencyException(e);
                    }
                }, _log);
            }
            else
            {
                var upd = Builders<PrimaryWalletMongoEntity>.Update
                    .Set(p => p.Address, wallet.Address)
                    .Set(p => p.CreatorType, wallet.CreatorType)
                    .Set(p => p.Updated, now)
                    .Inc(p => p.Version, 1);

                await CommandExtensions.WrapCommandAsync(async () =>
                {
                    var res = await _primaryWalletsCollection.UpdateOneAsync(
                        p => p.Id == lastKnown.Id && p.Version == lastKnown.Version, 
                        upd);

                    if (res.MatchedCount != 1)
                    {
                        throw new OptimisticConcurrencyException();
                    }
                }, _log);
            }
        }

        public async Task DeleteIfExistsAsync(string blockchainType, Guid clientId, string address)
        {
            var entityToDelete = (await _allWalletsCollection.WrapQueryAsync(_log,
                    query => query.Where(p => p.ClientId == clientId && p.BlockchainType == blockchainType && p.Address == address )))
                .SingleOrDefault();

            if (entityToDelete != null)
            {
                var primary = (await _primaryWalletsCollection.WrapQueryAsync(_log,
                        query => query.Where(p => p.ClientId == clientId && p.BlockchainType == blockchainType)))
                    .SingleOrDefault();

                if (primary?.Address == address)
                {
                    var nextWallet = (await _allWalletsCollection.WrapQueryAsync(_log,
                        query => query.Where(p => p.ClientId == clientId && p.BlockchainType == blockchainType && p.Address != address )
                            .OrderByDescending(p => p.Inserted)
                            .Take(1)))
                        .SingleOrDefault();

                    if (nextWallet != null)
                    {
                        await MakePrimaryAsync(nextWallet, primary);
                    }
                    else
                    {
                        await CommandExtensions.WrapCommandAsync(async () =>
                        {
                            var res = await _primaryWalletsCollection.DeleteOneAsync(p => p.Id == primary.Id && p.Version == primary.Version);

                            if (res.DeletedCount != 1)
                            {
                                throw new OptimisticConcurrencyException();
                            }
                        }, _log);
                    }
                }

                await CommandExtensions.WrapCommandAsync(async () =>
                {
                    var res = await _allWalletsCollection.DeleteOneAsync(p => p.Id == entityToDelete.Id && p.Version == entityToDelete.Version);

                    if (res.DeletedCount != 1)
                    {
                        throw new OptimisticConcurrencyException();
                    }

                }, _log);
            }
        }

        public async Task<bool> ExistsAsync(string blockchainType, string address)
        {
            return (await TryGetAsync(blockchainType, address)) != null;
        }

        public async Task<bool> ExistsAsync(string blockchainType, Guid clientId)
        {
            return (await TryGetPrimaryAsync(blockchainType, clientId)) != null;
        }

        public Task<(IReadOnlyCollection<WalletDto> Wallets, string ContinuationToken)> GetAllAsync(string blockchainType, 
            Guid clientId,
            int take,
            string continuationToken = null)
        {
            return GetDataWithContinuationTokenAsync(_allWalletsCollection, 
                query => query.Where(p => p.ClientId == clientId && p.BlockchainType == blockchainType), 
                take,
                continuationToken);
        }

        public Task<(IReadOnlyCollection<WalletDto> Wallets, string ContinuationToken)> GetAllPrimaryAsync(Guid clientId, 
            int take, 
            string continuationToken = null)
        {
            return GetDataWithContinuationTokenAsync(_primaryWalletsCollection,
                query => query.Where(p => p.ClientId == clientId), 
                take,
                continuationToken);
        }

        private async Task<(IReadOnlyCollection<WalletDto> Wallets, string ContinuationToken)> GetDataWithContinuationTokenAsync<T>(
                IMongoCollection<T> collection,
                Func<IMongoQueryable<T>, IMongoQueryable<T>> queryBuilder, 
                int take, 
                string continuationToken) where  T:IWallet
        {
            var skip = 0;
            if (!string.IsNullOrEmpty(continuationToken))
            {
                skip = ContinuationTokenModel.Deserialize(continuationToken).Skip;
            }

            var entities = (await collection.WrapQueryAsync(_log, query => queryBuilder(query).Skip(skip).Take(take))).ToList();

            var resultedContinuationToken = entities.Count < take
                ? null
                : new ContinuationTokenModel
                {
                    Skip = skip + entities.Count
                }.Serialize();
            
            return (entities.Select(p => ConvertEntityToDto(p)).ToList(), resultedContinuationToken);
        }

        public async Task<WalletDto> TryGetAsync(string blockchainType, string address)
        {
            var queryResult = (await _allWalletsCollection.WrapQueryAsync(_log,
                    query => query.Where(p => p.Address == address && p.BlockchainType == blockchainType)))
                .SingleOrDefault();
            
            if (queryResult != null)
            {
                return ConvertEntityToDto(queryResult);
            }

            return null;
        }

        public async Task<WalletDto> TryGetPrimaryAsync(string blockchainType, Guid clientId)
        {
            var queryResult = (await _primaryWalletsCollection.WrapQueryAsync(_log,
                    query => query.Where(p => p.ClientId == clientId && p.BlockchainType == blockchainType)))
                .SingleOrDefault();

            if (queryResult != null)
            {
                return ConvertEntityToDto(queryResult);
            }

            return null;
        }

        private static WalletDto ConvertEntityToDto(IWallet entity)
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
            await CreateAddressBlockchainTypeIndex();
            await CreateClientIdAddressIndex();
            await CreateClientIdBlockchainTypeIndex();
        }
        
        private async Task CreateAddressBlockchainTypeIndex()
        {
            var addressAsc = Builders<WalletMongoEntity>.IndexKeys.Ascending(p => p.Address);
            var blockchainTypeAsc = Builders<WalletMongoEntity>.IndexKeys.Ascending(p => p.BlockchainType);

            var combined = Builders<WalletMongoEntity>.IndexKeys.Combine(addressAsc, blockchainTypeAsc);

            await _allWalletsCollection.Indexes.CreateOneAsync(new CreateIndexModel<WalletMongoEntity>(combined,
                new CreateIndexOptions { Background = true, Unique = true }));
        }

        private async Task CreateClientIdAddressIndex()
        {
            var clientAsc = Builders<WalletMongoEntity>.IndexKeys.Ascending(p => p.ClientId);
            var blockchainTypeAsc = Builders<WalletMongoEntity>.IndexKeys.Ascending(p => p.BlockchainType);
            var addressAsc = Builders<WalletMongoEntity>.IndexKeys.Ascending(p => p.Address);

            var combined = Builders<WalletMongoEntity>.IndexKeys.Combine(clientAsc, blockchainTypeAsc, addressAsc);

            await _allWalletsCollection.Indexes.CreateOneAsync(new CreateIndexModel<WalletMongoEntity>(combined,
                new CreateIndexOptions { Background = true, Unique = true }));
        }

        private async Task CreateClientIdBlockchainTypeIndex()
        {
            var clientAsc = Builders<PrimaryWalletMongoEntity>.IndexKeys.Ascending(p => p.ClientId);
            var blockchainTypeAsc = Builders<PrimaryWalletMongoEntity>.IndexKeys.Ascending(p => p.BlockchainType);

            var combined = Builders<PrimaryWalletMongoEntity>.IndexKeys.Combine(clientAsc, blockchainTypeAsc);

            await _primaryWalletsCollection.Indexes.CreateOneAsync(new CreateIndexModel<PrimaryWalletMongoEntity>(combined,
                new CreateIndexOptions<PrimaryWalletMongoEntity> { Background = true, Unique = true }));
        }

        #endregion
    }
}
