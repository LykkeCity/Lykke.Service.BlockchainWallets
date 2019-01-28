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
using Lykke.AzureStorage.Tables.Paging;
using Lykke.Common.Log;
using Lykke.Service.BlockchainWallets.AzureRepositories.Utils;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Microsoft.AspNetCore.Razor.Language;


namespace Lykke.Service.BlockchainWallets.AzureRepositories
{
    public class BlockchainWalletsRepository : IBlockchainWalletsRepository
    {
        private const int _batchSize = 50;
        private readonly INoSQLTableStorage<AzureIndex> _addressIndexTable;
        private readonly INoSQLTableStorage<BlockchainWalletEntity> _walletsTable;
        private readonly INoSQLTableStorage<AzureIndex> _clientBlockchainTypeIndexTable;
        private readonly INoSQLTableStorage<AzureIndex> _clientLatestDepositsIndexTable;
        private readonly INoSQLTableStorage<BlockchainWalletEntity> _archiveTable;

        private BlockchainWalletsRepository(
            INoSQLTableStorage<AzureIndex> addressIndexTable,
            INoSQLTableStorage<BlockchainWalletEntity> walletsTable,
            INoSQLTableStorage<BlockchainWalletEntity> archiveTable,
            INoSQLTableStorage<AzureIndex> clientBlockchainTypeIndexTable,
            INoSQLTableStorage<AzureIndex> clientLatestDepositsIndexTable)
        {
            _addressIndexTable = addressIndexTable;
            _walletsTable = walletsTable;
            _archiveTable = archiveTable;
            _clientBlockchainTypeIndexTable = clientBlockchainTypeIndexTable;
            _clientLatestDepositsIndexTable = clientLatestDepositsIndexTable;
        }

        public static IBlockchainWalletsRepository Create(IReloadingManager<string> connectionString, ILogFactory logFactory)
        {
            const string tableName = "BlockchainWallets";
            const string archiveTableName = "BlockchainWalletsArchive";
            const string indexTableName = "BlockchainWalletsAddressIndex";
            const string clientBtIndexTableName = "BlockchainWalletsClientBtIndex";
            const string blockchainWalletsClientLatestDepositsIndex = "BlockchainWalletsClientLatestDepositsIndex";

            var addressIndexTable = AzureTableStorage<AzureIndex>.Create
            (
                connectionString,
                indexTableName,
                logFactory
            );

            var walletsTable = AzureTableStorage<BlockchainWalletEntity>.Create
            (
                connectionString,
                tableName,
                logFactory
            );

            var archiveWalletsTable = AzureTableStorage<BlockchainWalletEntity>.Create
            (
                connectionString,
                archiveTableName,
                logFactory
            );

            var clientBtIndexTable = AzureTableStorage<AzureIndex>.Create
            (
                connectionString,
                clientBtIndexTableName,
                logFactory
            );

            var blockchainWalletsClientLatestDepositsTable = AzureTableStorage<AzureIndex>.Create
            (
                connectionString,
                blockchainWalletsClientLatestDepositsIndex,
                logFactory
            );

            return new BlockchainWalletsRepository(addressIndexTable,
                walletsTable,
                archiveWalletsTable,
                clientBtIndexTable,
                blockchainWalletsClientLatestDepositsTable);
        }

        //clientLatestDepositIndexManualPartitionKey let it be null for common operations
        public async Task AddAsync(string blockchainType, Guid clientId, string address, 
            CreatorType createdBy, string clientLatestDepositIndexManualPartitionKey = null, bool addAsLatest = true)
        {
            var partitionKey = BlockchainWalletEntity.GetPartitionKey(blockchainType, clientId);
            var rowKey = BlockchainWalletEntity.GetRowKey(address);

            var clientLatestDepositIndexPartitionKey = GetClientLatestIndexPartitionKey(clientId);
            var clientLatestDepositIndexRowKey = GetClientLatestIndexRowKey(blockchainType);
            var (indexPartitionKey, indexRowKey) = GetAddressIndexKeys(blockchainType, address);
            var (clientBtIndexPartitionKey, clientBtIndexRowKey) = GetClientBlockchainTypeIndexKeys(blockchainType, clientId);

            clientBtIndexRowKey = clientLatestDepositIndexManualPartitionKey ?? clientBtIndexRowKey;

            await _addressIndexTable.InsertOrReplaceAsync(new AzureIndex(
                indexPartitionKey,
                indexRowKey,
                partitionKey,
                rowKey
            ));

            await _clientBlockchainTypeIndexTable.InsertOrReplaceAsync(new AzureIndex(
                clientBtIndexPartitionKey,
                clientBtIndexRowKey,
                partitionKey,
                rowKey
            ));

            if (addAsLatest)
            {
                await _clientLatestDepositsIndexTable.InsertOrReplaceAsync(new AzureIndex(
                    clientLatestDepositIndexPartitionKey,
                    clientLatestDepositIndexRowKey,
                    partitionKey,
                    rowKey
                ));
            }

            // Wallet entity

            await _walletsTable.InsertOrReplaceAsync(new BlockchainWalletEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,

                Address = address,
                ClientId = clientId,
                IntegrationLayerId = blockchainType,
                CreatedBy = createdBy
            });
        }

        public async Task DeleteIfExistsAsync(string blockchainType, Guid clientId, string address)
        {
            var partitionKey = BlockchainWalletEntity.GetPartitionKey(blockchainType, clientId);
            var rowKey = BlockchainWalletEntity.GetRowKey(address);
            var (indexPartitionKey, indexRowKey) = GetAddressIndexKeys(blockchainType, address);
            var addressIndex = await _addressIndexTable.GetDataAsync(indexPartitionKey, indexRowKey);

            var clientLatestDepositIndexPartitionKey = GetClientLatestIndexPartitionKey(clientId);
            var clientLatestDepositIndexRowKey = GetClientLatestIndexRowKey(blockchainType);

            var latestDeposit = await _clientLatestDepositsIndexTable.GetDataAsync(clientLatestDepositIndexPartitionKey,
                clientLatestDepositIndexRowKey);
            var wallet = await _walletsTable.GetDataAsync(partitionKey, rowKey);
            var (clientBtIndexPartitionKey, clientBtIndexRowKey) = GetClientBlockchainTypeIndexKeys(blockchainType, clientId);

            await _archiveTable.InsertOrReplaceAsync(wallet);
            await _clientBlockchainTypeIndexTable.DeleteIfExistAsync(clientBtIndexPartitionKey, clientBtIndexRowKey);
            await _addressIndexTable.DeleteIfExistAsync(indexPartitionKey, indexRowKey);

            if (wallet?.Address == latestDeposit?.PrimaryRowKey)
            {
                await _clientLatestDepositsIndexTable.DeleteIfExistAsync(clientLatestDepositIndexPartitionKey,
                    clientLatestDepositIndexRowKey,
                    (index) => latestDeposit?.ETag == index.ETag);

                AzureIndex newMostRecent = null;
                string cToken = null;

                do
                {
                    var previousDeposits =
                        await GetClientBlockchainTypeIndices(blockchainType, clientId, _batchSize, cToken);
                    var toDelete = previousDeposits.Entities.FirstOrDefault(x => x.PrimaryRowKey == latestDeposit?.PrimaryRowKey);
                    if (!string.IsNullOrEmpty(toDelete.PrimaryRowKey) &&
                        !string.IsNullOrEmpty(toDelete.PrimaryPartitionKey))
                    {
                        await _clientBlockchainTypeIndexTable.DeleteIfExistAsync(toDelete.PartitionKey, toDelete.RowKey);
                    }
                    if (newMostRecent == null)
                    {
                        newMostRecent = previousDeposits.Entities.FirstOrDefault(x => x.PrimaryRowKey != latestDeposit?.PrimaryRowKey);
                    }
                } while (cToken != null);


                if (newMostRecent != null)
                {
                    await _clientLatestDepositsIndexTable.InsertOrReplaceAsync(new AzureIndex(
                        clientLatestDepositIndexPartitionKey,
                        clientLatestDepositIndexRowKey,
                        newMostRecent.PrimaryPartitionKey,
                        newMostRecent.PrimaryRowKey));
                }
            }
            else
            {
                string cToken = null;

                do
                {
                    var previousDeposits =
                        await  GetClientBlockchainTypeIndices(blockchainType, clientId, _batchSize, cToken);
                    var toDelete = previousDeposits.Entities.FirstOrDefault(x => x.PrimaryRowKey == wallet?.Address);
                    if (toDelete != null 
                        && !string.IsNullOrEmpty(toDelete.PrimaryPartitionKey)
                        && !string.IsNullOrEmpty(toDelete.PrimaryRowKey))
                    {
                        await _clientBlockchainTypeIndexTable.DeleteIfExistAsync(toDelete.PartitionKey, toDelete.RowKey);
                        break;
                    }
                } while (cToken != null);
            }

            await _walletsTable.DeleteIfExistAsync(wallet?.PartitionKey, wallet?.RowKey);
        }

        public async Task<bool> ExistsAsync(string blockchainType, string address)
        {
            return await TryGetAsync(blockchainType, address) != null;
        }

        public async Task<bool> ExistsAsync(string blockchainType, Guid clientId)
        {
            return await TryGetAsync(blockchainType, clientId) != null;
        }

        public async Task<(IEnumerable<WalletDto> Wallets, string ContinuationToken)> GetAllAsync(string blockchainType, Guid clientId, int take, string continuationToken = null)
        {
            var indexes = await GetForClientAndBlockchainTypeIndicesAsync(blockchainType, clientId, take, continuationToken);
            var keys = indexes.wallets.Select(x => Tuple.Create(x.WalletPartitionKey, x.WalletRowKey));

            var wallets = (await _walletsTable.GetDataAsync(keys, take))
                .Select(ConvertEntityToDto);

            var walletDictionay = wallets.ToDictionary(x => x.Address);
            var sortedWallets = indexes.wallets.Select(x =>
            {
                if (walletDictionay.TryGetValue(x.WalletRowKey, out var walet))
                {
                    return walet;
                }

                return null;
            }).Where(x => x != null);

            return (sortedWallets, indexes.continuationToken);
        }

        public async Task<(IEnumerable<WalletDto> Wallets, string ContinuationToken)> GetAllAsync(Guid clientId, int take, string continuationToken = null)
        {
            var indexes = await GetForClientIndicesAsync(clientId, take, continuationToken);
            var keys = indexes.wallets.Select(x =>  (partitionKey: x.WalletPartitionKey, rowKey: x.WalletRowKey));
            var wallets = (await keys.SelectAsync(p => _walletsTable.GetDataAsync(p.partitionKey, p.rowKey)))
                    .Select(ConvertEntityToDto);

            var walletDictionay = wallets.ToDictionary(x => x.Address);
            var sortedWallets = indexes.wallets.Select(x =>
            {
                if (walletDictionay.TryGetValue(x.WalletRowKey, out var walet))
                {
                    return walet;
                }

                return null;
            }).Where(x => x != null);

            return (sortedWallets, indexes.continuationToken);
        }

        public async Task<WalletDto> TryGetAsync(string blockchainType, string address)
        {
            var (partitionKey, rowKey) = GetAddressIndexKeys(blockchainType, address);
            var index = await _addressIndexTable.GetDataAsync(partitionKey, rowKey);
            var entity = await _walletsTable.GetDataAsync(index);

            return entity != null
                ? ConvertEntityToDto(entity)
                : null;
        }

        public async Task<WalletDto> TryGetAsync(string blockchainType, Guid clientId)
        {
            var (partitionKey, rowKey) = GetClientBlockchainTypeIndexKeys(blockchainType, clientId);
            var indexes = await _clientBlockchainTypeIndexTable.GetDataWithContinuationTokenAsync(partitionKey, 10, null);
            var latestIndex = indexes.Entities.FirstOrDefault();
            var entity = await _walletsTable.GetDataAsync(latestIndex);

            return entity != null
                ? ConvertEntityToDto(entity)
                : null;
        }

        public async Task<(IEnumerable<WalletDto> Wallets, string ContinuationToken)> GetAllAsync(int take, string continuationToken)
        {
            IEnumerable<BlockchainWalletEntity> entities;

            (entities, continuationToken) = await _walletsTable.GetDataWithContinuationTokenAsync(take, continuationToken);

            var wallets = entities.Select(ConvertEntityToDto);

            return (wallets, continuationToken);
        }

        #region Keys

        private static string GetClientLatestIndexPartitionKey(Guid clientId)
        {
            return clientId.ToString();
        }

        private static string GetClientLatestIndexRowKey(string blockchainType)
        {
            return blockchainType;
        }

        private static (string PartitionKey, string RowKey) GetAddressIndexKeys(BlockchainWalletEntity wallet)
        {
            return GetAddressIndexKeys(wallet.IntegrationLayerId, wallet.Address);
        }

        private static (string PartitionKey, string RowKey) GetAddressIndexKeys(string blockchainType, string address)
        {
            var partitionKey = $"{blockchainType}-{address.CalculateHexHash32(3)}";
            var rowKey = address;

            return (partitionKey, rowKey);
        }

        private static (string PartitionKey, string RowKey) GetClientIndexKeys(BlockchainWalletEntity wallet)
        {
            return GetClientIndexKeys(wallet.ClientId);
        }

        private static (string PartitionKey, string RowKey) GetClientIndexKeys(Guid clientId)
        {
            var partitionKey = GetClientPartitionKey(clientId);
            var rowKey = LogTailRowKeyGenerator.GenerateRowKey();

            return (partitionKey, rowKey);
        }

        private static string GetClientPartitionKey(Guid clientId)
        {
            var partitionKey = $"{clientId}";

            return partitionKey;
        }

        private static (string PartitionKey, string RowKey) GetClientBlockchainTypeIndexKeys(
            string blockchainType, Guid clientId)
        {
            var partitionKey = GetClientBlockchainTypePartitionKey(blockchainType, clientId);
            var rowKey = LogTailRowKeyGenerator.GenerateRowKey();

            return (partitionKey, rowKey);
        }

        private static string GetClientBlockchainTypePartitionKey(string blockchainType, Guid clientId)
        {
            var partitionKey = $"{blockchainType}-{clientId}";

            return partitionKey;
        }

        #endregion

        private static WalletDto ConvertEntityToDto(BlockchainWalletEntity entity)
        {
            return new WalletDto
            {
                Address = entity.Address,
                BlockchainType = entity.IntegrationLayerId,
                ClientId = entity.ClientId,
                CreatorType = entity.CreatedBy
            };
        }

        private async Task<(IEnumerable<(string WalletPartitionKey, string WalletRowKey)> wallets, string continuationToken)>
            GetForClientIndicesAsync(Guid clientId, int take, string continuationToken)
        {
            var partitionKey = GetClientLatestIndexPartitionKey(clientId);
            var indexes = await _clientLatestDepositsIndexTable.GetDataWithContinuationTokenAsync(partitionKey, take, continuationToken);
            var values = indexes.Entities.Select(x => (x.PrimaryPartitionKey, x.PrimaryRowKey));

            return (values, indexes.ContinuationToken);
        }

        private async Task<(IEnumerable<(string WalletPartitionKey, string WalletRowKey)> wallets, string continuationToken)>
            GetForClientAndBlockchainTypeIndicesAsync(string blockchainType,
                Guid clientId,
                int take,
                string continuationToken)
        {
            var indexes = await GetClientBlockchainTypeIndices(blockchainType, clientId, take, continuationToken);
            var values = indexes.Entities.Select(x => (x.PrimaryPartitionKey, x.PrimaryRowKey));

            return (values, indexes.ContinuationToken);
        }

        //Use only in Tools
        internal async Task<(IEnumerable<AzureIndex> Entities, string ContinuationToken)>
            GetClientBlockchainTypeIndices(string blockchainType, Guid clientId, int take,
            string continuationToken)
        {
            var partitionKey = GetClientBlockchainTypePartitionKey(blockchainType, clientId);
            var indexes =
                await _clientBlockchainTypeIndexTable.GetDataWithContinuationTokenAsync(partitionKey, take, continuationToken);

            return indexes;
        }
    }
}
