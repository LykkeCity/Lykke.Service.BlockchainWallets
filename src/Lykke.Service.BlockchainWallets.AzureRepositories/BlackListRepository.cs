using AzureStorage;
using AzureStorage.Tables;
using Common;
using Lykke.Common.Log;
using Lykke.Service.BlockchainWallets.AzureRepositories.Entities;
using Lykke.Service.BlockchainWallets.Core.DTOs.Validation;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.Exceptions;

namespace Lykke.Service.BlockchainWallets.AzureRepositories
{
    public class BlackListRepository : IBlackListRepository
    {
        private readonly INoSQLTableStorage<BlackListEntity> _storage;

        public static IBlackListRepository Create(IReloadingManager<string> connectionString, ILogFactory logFactory)
        {
            var storage = AzureTableStorage<BlackListEntity>.Create(
                connectionString,
                "CashoutBlackList",
                logFactory);

            return new BlackListRepository(storage);
        }

        private BlackListRepository(INoSQLTableStorage<BlackListEntity> storage)
        {
            _storage = storage;
        }

        public async Task<BlackListModel> GetOrAddAsync(string blockchainType, string blockedAddress, Func<BlackListModel> newAggregateFactory)
        {
            var partitionKey = BlackListEntity.GetPartitionKey(blockchainType);
            var rowKey = BlackListEntity.GetRowKey(blockedAddress);

            var startedEntity = await _storage.GetOrInsertAsync(
                partitionKey,
                rowKey,
                () =>
                {
                    var newAggregate = newAggregateFactory();

                    return BlackListEntity.FromDomain(newAggregate);
                });

            return startedEntity.ToDomain();
        }

        public async Task<BlackListModel> TryGetAsync(string blockchainType, string blockedAddress)
        {
            var partitionKey = BlackListEntity.GetPartitionKey(blockchainType);
            var rowKey = BlackListEntity.GetRowKey(blockedAddress);

            var entity = await _storage.GetDataAsync(partitionKey, rowKey);

            return entity?.ToDomain();
        }

        public async Task<(IEnumerable<BlackListModel>, string continuationToken)> TryGetAllAsync(string blockchainType, int take, string continuationToken = null)
        {
            if (!string.IsNullOrEmpty(continuationToken))
            {
                try
                {
                    Newtonsoft.Json.JsonConvert.DeserializeObject<TableContinuationToken>(Utils.HexToString(continuationToken));
                }
                catch
                {
                    throw new OperationException($"{continuationToken} continuationToken is not valid", OperationErrorCode.None);
                }
            }

            var partitionKey = BlackListEntity.GetPartitionKey(blockchainType);

            var (entities, newToken) = await _storage.GetDataWithContinuationTokenAsync(partitionKey, take, continuationToken);

            return (entities?.Select(x => x.ToDomain()), newToken);
        }

        public async Task SaveAsync(BlackListModel aggregate)
        {
            var entity = BlackListEntity.FromDomain(aggregate);

            await _storage.InsertOrReplaceAsync(entity);
        }

        public async Task DeleteAsync(string blockchainType, string blockedAddress)
        {
            var partitionKey = BlackListEntity.GetPartitionKey(blockchainType);
            var rowKey = BlackListEntity.GetRowKey(blockedAddress);

            var existingEntity = await _storage.GetDataAsync(partitionKey, rowKey);

            if (existingEntity == null)
            {
                throw new OperationException($"Entity with address {blockedAddress} does not exist", OperationErrorCode.None);
            }

            await _storage.DeleteIfExistAsync(partitionKey, rowKey);
        }
    }
}
