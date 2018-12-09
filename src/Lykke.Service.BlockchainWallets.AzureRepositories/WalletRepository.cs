using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using Common;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;
using Lykke.Common.Log;
using Lykke.Service.BlockchainWallets.AzureRepositories.Entities;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.Repositories;


namespace Lykke.Service.BlockchainWallets.AzureRepositories
{
    [Obsolete]
    public class WalletRepository : IWalletRepository
    {
        private readonly INoSQLTableStorage<AzureIndex> _clientIndexTable;
        private readonly INoSQLTableStorage<AzureIndex> _addressIndexTable;
        private readonly INoSQLTableStorage<WalletEntity> _walletsTable;

        private WalletRepository(
            INoSQLTableStorage<AzureIndex> addressIndexTable,
            INoSQLTableStorage<WalletEntity> walletsTable,
            INoSQLTableStorage<AzureIndex> clientIndexTable)
        {
            _addressIndexTable = addressIndexTable;
            _walletsTable = walletsTable;
            _clientIndexTable = clientIndexTable;
        }


        public static IWalletRepository Create(IReloadingManager<string> connectionString, ILogFactory logFactory)
        {
            const string tableName = "Wallets";
            const string indexTableName = "WalletsAddressIndex";
            const string clientIndexTableName = "WalletsClientIndex";

            var addressIndexTable = AzureTableStorage<AzureIndex>.Create
            (
                connectionString,
                indexTableName,
                logFactory
            );

            var clientAddressIndexTable = AzureTableStorage<AzureIndex>.Create
            (
                connectionString,
                clientIndexTableName,
                logFactory
            );

            var walletsTable = AzureTableStorage<WalletEntity>.Create
            (
                connectionString,
                tableName,
                logFactory
            );

            return new WalletRepository(addressIndexTable, walletsTable, clientAddressIndexTable);
        }

        #region Keys

        private static (string PartitionKey, string RowKey) GetAddressIndexKeys(WalletEntity wallet)
        {
            return GetAddressIndexKeys(wallet.IntegrationLayerId, wallet.Address);
        }

        private static (string PartitionKey, string RowKey) GetAddressIndexKeys(string blockchainType, string address)
        {
            var partitionKey = $"{blockchainType}-{address.CalculateHexHash32(3)}";
            var rowKey = address;

            return (partitionKey, rowKey);
        }

        private static (string PartitionKey, string RowKey) GetClientIndexKeys(WalletEntity wallet)
        {
            return GetClientIndexKeys(wallet.IntegrationLayerId, wallet.AssetId, wallet.ClientId);
        }

        private static (string PartitionKey, string RowKey) GetClientIndexKeys(string blockchainType, string assetId, Guid clientId)
        {
            var partitionKey = GetClientPartitionKey(clientId);
            var rowKey = $"{blockchainType}-{assetId}";

            return (partitionKey, rowKey);
        }

        private static string GetClientPartitionKey(Guid clientId)
        {
            var partitionKey = $"{clientId}";

            return partitionKey;
        }

        #endregion

        public async Task AddAsync(string blockchainType, string assetId, Guid clientId, string address)
        {
            var partitionKey = WalletEntity.GetPartitionKey(blockchainType, assetId, clientId);
            var rowKey = WalletEntity.GetRowKey(clientId);

            // Address index

            var (indexPartitionKey, indexRowKey) = GetAddressIndexKeys(blockchainType, address);
            var (clientIndexPartitionKey, clientIndexRowKey) = GetClientIndexKeys(blockchainType, assetId, clientId);

            await _addressIndexTable.InsertOrReplaceAsync(new AzureIndex(
                indexPartitionKey,
                indexRowKey,
                partitionKey,
                rowKey
            ));

            await _clientIndexTable.InsertOrReplaceAsync(new AzureIndex(
                clientIndexPartitionKey,
                clientIndexRowKey,
                partitionKey,
                rowKey
            ));

            // Wallet entity

            await _walletsTable.InsertOrReplaceAsync(new WalletEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,

                Address = address,
                AssetId = assetId,
                ClientId = clientId,
                IntegrationLayerId = blockchainType
            });
        }

        public async Task DeleteIfExistsAsync(string blockchainType, string assetId, Guid clientId)
        {
            var partitionKey = WalletEntity.GetPartitionKey(blockchainType, assetId, clientId);
            var rowKey = WalletEntity.GetRowKey(clientId);

            var wallet = await _walletsTable.GetDataAsync(partitionKey, rowKey);

            if (wallet != null)
            {
                var (indexPartitionKey, indexRowKey) = GetAddressIndexKeys(wallet);
                var (clientIndexPartitionKey, clientIndexRowKey) = GetClientIndexKeys(wallet);

                await _walletsTable.DeleteIfExistAsync(wallet.PartitionKey, wallet.RowKey);
                await _addressIndexTable.DeleteIfExistAsync(indexPartitionKey, indexRowKey);
                await _clientIndexTable.DeleteIfExistAsync(clientIndexPartitionKey, clientIndexRowKey);
            }
        }

        public async Task<bool> ExistsAsync(string blockchainType, string address)
        {
            return await TryGetAsync(blockchainType, address) != null;
        }

        public async Task<bool> ExistsAsync(string blockchainType, string assetId, Guid clientId)
        {
            return await TryGetAsync(blockchainType, assetId, clientId) != null;
        }

        public async Task<(IEnumerable<WalletDto> Wallets, string ContinuationToken)> GetAllAsync(Guid clientId, int take, string continuationToken)
        {
            var indexes = await GetForClientIndicesAsync(clientId, take, continuationToken);
            var keys = indexes.wallets.Select(x => Tuple.Create(x.WalletPartitionKey, x.WalletRowKey));

            var wallets = (await _walletsTable.GetDataAsync(keys, take))
                .Select(ConvertEntityToDto);

            return (wallets, indexes.continuationToken);
        }

        // NB! This method should be used only by conversion utility or in similar cases.
        [Obsolete("Do not use it")]
        internal async Task<(IEnumerable<WalletDto> Wallets, string ContinuationToken)> GetAsync(string blockchainType, string assetId, int take, string continuationToken)
        {
            var filterCondition = TableQuery.CombineFilters
            (
                TableQuery.GenerateFilterCondition("IntegrationLayerId", QueryComparisons.Equal, blockchainType),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("AssetId", QueryComparisons.Equal, assetId)
            );

            var query = new TableQuery<WalletEntity>().Where(filterCondition);

            IEnumerable<WalletEntity> entities;

            (entities, continuationToken) = await _walletsTable.GetDataWithContinuationTokenAsync(query, take, continuationToken);

            return (entities.Select(ConvertEntityToDto), continuationToken);
        }

        public async Task<WalletDto> TryGetAsync(string blockchainType, string address)
        {
            var (indexPartitionKey, indexRowKey) = GetAddressIndexKeys(blockchainType, address);

            var index = await _addressIndexTable.GetDataAsync
            (
                partition: indexPartitionKey,
                row: indexRowKey
            );

            if (index != null)
            {
                var entity = await _walletsTable.GetDataAsync(index.PrimaryPartitionKey, index.PrimaryRowKey);

                return entity != null
                    ? ConvertEntityToDto(entity)
                    : null;
            }

            return null;
        }

        public async Task<WalletDto> TryGetAsync(string blockchainType, string assetId, Guid clientId)
        {
            var partitionKey = WalletEntity.GetPartitionKey(blockchainType, assetId, clientId);
            var rowKey = WalletEntity.GetRowKey(clientId);
            var entity = await _walletsTable.GetDataAsync(partitionKey, rowKey);
            
            return entity != null 
                ? ConvertEntityToDto(entity) 
                : null;
        }
        
        private async Task<(IEnumerable<(string WalletPartitionKey, string WalletRowKey)> wallets, string continuationToken)> GetForClientIndicesAsync(Guid clientId, int take, string continuationToken)
        {
            var partitionKey = GetClientPartitionKey(clientId);
            var indexes = await _clientIndexTable.GetDataWithContinuationTokenAsync(partitionKey, take, continuationToken);
            var values = indexes.Entities.Select(x => (x.PrimaryPartitionKey, x.PrimaryRowKey));

            return (values, indexes.ContinuationToken);
        }

        // NB! This method should be used only by conversion utility or in similar cases.
        public async Task<(IEnumerable<WalletDto> Wallets, string ContinuationToken)> GetAllAsync(int take, string continuationToken)
        {
            IEnumerable<WalletEntity> entities;

            (entities, continuationToken) = await _walletsTable.GetDataWithContinuationTokenAsync(take, continuationToken);

            var wallets = entities.Select(ConvertEntityToDto);

            return (wallets, continuationToken);
        }

        private static WalletDto ConvertEntityToDto(WalletEntity entity)
        {
            return new WalletDto
            {
                Address = entity.Address,
                AssetId = entity.AssetId,
                BlockchainType = entity.IntegrationLayerId,
                ClientId = entity.ClientId
            };
        }
    }
}
