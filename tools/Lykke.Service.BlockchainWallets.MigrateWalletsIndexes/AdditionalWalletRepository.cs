using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using Common;
using Lykke.Common.Log;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.SettingsReader;
using MoreLinq;

namespace Lykke.Service.BlockchainWallets.MigrateWalletsIndexes
{
    public class AdditionalWalletRepository
    {
        private readonly INoSQLTableStorage<AzureIndex> _addressIndexTable;
        private readonly INoSQLTableStorage<AdditionalWalletEntity> _additionalWalletsTable;

        private AdditionalWalletRepository(
            INoSQLTableStorage<AzureIndex> addressIndexTable,
            INoSQLTableStorage<AdditionalWalletEntity> additionalWalletsTable)
        {
            _addressIndexTable = addressIndexTable;
            _additionalWalletsTable = additionalWalletsTable;
        }


        public static AdditionalWalletRepository Create(IReloadingManager<string> connectionString, ILogFactory logFactory)
        {
            const string tableName = "AdditionalWallets";
            const string indexTableName = "AdditionalWalletsAddressIndex";

            var addressIndexTable = AzureTableStorage<AzureIndex>.Create
            (
                connectionString,
                indexTableName,
                logFactory
            );

            var additionalWalletsTable = AzureTableStorage<AdditionalWalletEntity>.Create
            (
                connectionString,
                tableName,
                logFactory
            );

            return new AdditionalWalletRepository(addressIndexTable, additionalWalletsTable);
        }


        private static (string PartitionKey, string RowKey) GetAddressIndexKeys(string blockchainType, string address)
        {
            var partitionKey = $"{blockchainType}-{address.CalculateHexHash32(3)}";
            var rowKey = address;

            return (partitionKey, rowKey);
        }

        public async Task DeleteAllAddressIndexesAsync()
        {
            string continuation = null;
            do
            {
                IEnumerable<AzureIndex> indexes;
                (indexes, continuation) = await _addressIndexTable.GetDataWithContinuationTokenAsync(100, continuation);
                foreach (var batch in indexes.Batch(10))
                {
                    await Task.WhenAll(batch.Select(o => _addressIndexTable.DeleteAsync(o)));
                }
            } while (continuation != null);
        }


        public async Task<(IEnumerable<AdditionalWalletEntity> Wallets, string ContinuationToken)> GetAsync(int take, string continuationToken)
        {
            IEnumerable<AdditionalWalletEntity> entities;

            (entities, continuationToken) = await _additionalWalletsTable.GetDataWithContinuationTokenAsync(take, continuationToken);

            return (entities, continuationToken);
        }


        public Task AddAddressIndex(AdditionalWalletEntity entity)
        {
            var (indexPartitionKey, indexRowKey) = GetAddressIndexKeys(entity.IntegrationLayerId, entity.Address);
            return _addressIndexTable.InsertOrReplaceAsync(new AzureIndex(
                indexPartitionKey,
                indexRowKey,
                entity.PartitionKey,
                entity.RowKey
            ));
        }

    }
}
