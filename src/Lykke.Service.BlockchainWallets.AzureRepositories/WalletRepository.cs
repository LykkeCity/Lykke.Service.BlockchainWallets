using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using Common;
using Common.Log;
using Lykke.Service.BlockchainWallets.Core.Domain.Wallet;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;

namespace Lykke.Service.BlockchainWallets.AzureRepositories
{
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


        public static IWalletRepository Create(IReloadingManager<string> connectionString, ILog log)
        {
            const string tableName = "Wallets";
            const string indexTableName = "WalletsAddressIndex";
            const string clientIndexTableName = "WalletsClientIndex";

            var addressIndexTable = AzureTableStorage<AzureIndex>.Create
            (
                connectionString,
                indexTableName,
                log
            );

            var clientAddressIndexTable = AzureTableStorage<AzureIndex>.Create
            (
                connectionString,
                clientIndexTableName,
                log
            );

            var walletsTable = AzureTableStorage<WalletEntity>.Create
            (
                connectionString,
                tableName,
                log
            );

            return new WalletRepository(addressIndexTable, walletsTable, clientAddressIndexTable);
        }

        #region Keys

        private static (string PartitionKey, string RowKey) GetAddressIndexKeys(IWallet wallet)
        {
            return GetAddressIndexKeys(wallet.IntegrationLayerId, wallet.AssetId, wallet.Address);
        }

        private static (string PartitionKey, string RowKey) GetAddressIndexKeys(string integrationLayerId, string assetId, string address)
        {
            var partitionKey = $"{integrationLayerId}-{assetId}-{address.CalculateHexHash32(3)}";
            var rowKey = address;

            return (partitionKey, rowKey);
        }

        private static (string PartitionKey, string RowKey) GetClientIndexKeys(IWallet wallet)
        {
            return GetClientIndexKeys(wallet.IntegrationLayerId, wallet.AssetId, wallet.ClientId);
        }

        private static (string PartitionKey, string RowKey) GetClientIndexKeys(string integrationLayerId, string assetId, Guid clientId)
        {
            var partitionKey = GetClientPartitionKey(clientId);
            var rowKey = $"{integrationLayerId}-{assetId}";

            return (partitionKey, rowKey);
        }

        private static string GetClientPartitionKey(Guid clientId)
        {
            var partitionKey = $"{clientId}";
            return partitionKey;
        }

        #endregion

        public async Task AddAsync(string integrationLayerId, string assetId, Guid clientId, string address)
        {
            var partitionKey = WalletEntity.GetPartitionKey(integrationLayerId, assetId, clientId);
            var rowKey = WalletEntity.GetRowKey(clientId);

            // Address index

            (var indexPartitionKey, var indexRowKey) = GetAddressIndexKeys(integrationLayerId, assetId, address);
            (var clientIndexPartitionKey, var clientIndexRowKey) = GetClientIndexKeys(integrationLayerId, assetId, clientId);

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
                IntegrationLayerId = integrationLayerId
            });
        }

        public async Task DeleteIfExistsAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            var partitionKey = WalletEntity.GetPartitionKey(integrationLayerId, assetId, clientId);
            var rowKey = WalletEntity.GetRowKey(clientId);

            var wallet = await _walletsTable.GetDataAsync(partitionKey, rowKey);

            if (wallet != null)
            {
                (var indexPartitionKey, var indexRowKey) = GetAddressIndexKeys(wallet);
                (var clientIndexPartitionKey, var clientIndexRowKey) = GetClientIndexKeys(wallet);

                await _walletsTable.DeleteIfExistAsync(wallet.PartitionKey, wallet.RowKey);
                await _addressIndexTable.DeleteIfExistAsync(indexPartitionKey, indexRowKey);
                await _clientIndexTable.DeleteIfExistAsync(clientIndexPartitionKey, clientIndexRowKey);
            }
        }

        public async Task<bool> ExistsAsync(string integrationLayerId, string assetId, string address)
        {
            return await TryGetAsync(integrationLayerId, assetId, address) != null;
        }

        public async Task<bool> ExistsAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            return await TryGetAsync(integrationLayerId, assetId, clientId) != null;
        }

        // NB! This method should be used only by conversion utility or in similar cases.
        internal async Task<(IEnumerable<IWallet> wallets, string continuationToken)> GetAsync(string integrationLayerId, string assetId, int take, string continuationToken)
        {
            var filterCondition = TableQuery.CombineFilters
            (
                TableQuery.GenerateFilterCondition("IntegrationLayerId", QueryComparisons.Equal, integrationLayerId),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("AssetId", QueryComparisons.Equal, assetId)
            );

            var query = new TableQuery<WalletEntity>().Where(filterCondition);

            return await _walletsTable.GetDataWithContinuationTokenAsync(query, take, continuationToken);
        }

        public async Task<IWallet> TryGetAsync(string integrationLayerId, string assetId, string address)
        {
            (var indexPartitionKey, var indexRowKey) = GetAddressIndexKeys(integrationLayerId, assetId, address);

            var index = await _addressIndexTable.GetDataAsync(indexPartitionKey, indexRowKey);

            if (index != null)
            {
                var wallet = await _walletsTable.GetDataAsync(index.PrimaryPartitionKey, index.PrimaryRowKey);

                return wallet;
            }

            return null;
        }

        public async Task<IWallet> TryGetAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            var partitionKey = WalletEntity.GetPartitionKey(integrationLayerId, assetId, clientId);
            var rowKey = WalletEntity.GetRowKey(clientId);

            return await _walletsTable.GetDataAsync(partitionKey, rowKey);
        }

        public async Task<(IEnumerable<IWallet>, string continuationToken)> TryGetForClientAsync(Guid clientId, int take, string continuationToken)
        {
            var indexes = await GetForClientIndixesAsync(clientId, take, continuationToken);
            var keys = indexes.wallets.Select(x => Tuple.Create<string, string>(x.walletPartitionKey, x.walletRowKey));
            var wallets = await _walletsTable.GetDataAsync(keys, take);

            return (wallets, indexes.continuationToken);
        }

        // NB! This method should be used only by conversion utility or in similar cases.
        internal async Task<(IEnumerable<(string walletPartitionKey, string walletRowKey)> wallets, string continuationToken)> 
            GetForClientIndixesAsync(Guid clientId, int take, string continuationToken)
        {
            var partitionKey = GetClientPartitionKey(clientId);
            var indexes = await _clientIndexTable.GetDataWithContinuationTokenAsync(partitionKey, take, continuationToken);
            var values = indexes.Entities.Select(x => (x.PrimaryPartitionKey, x.PrimaryRowKey));

            return (values, continuationToken);
        }

        // NB! This method should be used only by conversion utility or in similar cases.
        internal async Task<(IEnumerable<IWallet> wallets, string continuationToken)>
            GetAllAsync(int take, string continuationToken)
        {
            var indexes = await _walletsTable.GetDataWithContinuationTokenAsync(take, continuationToken);

            return (indexes.Entities, indexes.ContinuationToken);
        }
    }
}
