using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common;
using Lykke.Common.Log;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.SettingsReader;

namespace Lykke.Service.BlockchainWallets.AzureRepositories.Backup
{
    public class BlockchainWalletsBackupRepository: IBlockchainWalletsBackupRepository
    {
        private readonly INoSQLTableStorage<BlockchainWalletBackupEntity> _storage;
        private readonly INoSQLTableStorage<BlockchainWalletsBackupIsPrimaryChangesIndex> _isPrimaryIndexStorage;
        private readonly INoSQLTableStorage<BlockchainWalletsArchiveIndex> _isDeletedStorage;

        private BlockchainWalletsBackupRepository(INoSQLTableStorage<BlockchainWalletBackupEntity> storage,
            INoSQLTableStorage<BlockchainWalletsBackupIsPrimaryChangesIndex> isPrimaryStorage,
            INoSQLTableStorage<BlockchainWalletsArchiveIndex> isDeletedStorage)
        {
            _storage = storage;
            _isPrimaryIndexStorage = isPrimaryStorage;
            _isDeletedStorage = isDeletedStorage;
        }

        public static BlockchainWalletsBackupRepository Create(IReloadingManager<string> connectionString,
            ILogFactory logFactory)
        {
            return new BlockchainWalletsBackupRepository(AzureTableStorage<BlockchainWalletBackupEntity>.Create
            (
                connectionString,
                "BlockchainWalletsBackupV3",
                logFactory
            ), AzureTableStorage<BlockchainWalletsBackupIsPrimaryChangesIndex>.Create
            (
                connectionString,
                "BlockchainWalletsBackupV3IsPrimaryChangesIndex",
                logFactory
            ), AzureTableStorage<BlockchainWalletsArchiveIndex>.Create
            (
                connectionString,
                "BlockchainWalletsBackupV3ArchiveIndex",
                logFactory
            ));
        }

        public async Task AddAsync(string blockchainType, Guid clientId, string address, CreatorType createdBy)
        {
            await _storage.InsertOrReplaceAsync(new BlockchainWalletBackupEntity
            {
                ClientId = clientId,
                Address = address,
                PartitionKey = BlockchainWalletBackupEntity.GetPartitionKey(clientId),
                RowKey = BlockchainWalletBackupEntity.GetRowKey(address, blockchainType),
                CreatedBy = createdBy,
                BlockchainType = blockchainType,
            });
        }

        public async Task<(IReadOnlyCollection<(string blockchainType, Guid clientId, string address, CreatorType createdBy, bool isPrimary)> Entities, string ContinuationToken)>
            GetDataWithContinuationTokenAsync(int take, string continuationToken)
        {
            var queryResult = await _storage.GetDataWithContinuationTokenAsync(take, continuationToken);

            var mapped = await queryResult.Entities.SelectAsync(async p => (blockchainType: p.BlockchainType,
                clientId: p.ClientId,
                address: p.Address,
                createdBy: p.CreatedBy,
                isPrimary: await IsPrimaryWallet(p.ClientId, p.BlockchainType, p.Address),
                IsDeleted: await IsDeleted(p.ClientId, p.Address, p.BlockchainType)));

            return (mapped.Where(p => !p.IsDeleted).Select(p=> (p.blockchainType, p.clientId, p.address, p.createdBy, p.isPrimary)).ToList(), 
                queryResult.ContinuationToken);
        }

        public async Task SetPrimaryWalletAsync(string blockchainType, Guid clientId, string address, int version)
        {
            await _isPrimaryIndexStorage.InsertOrReplaceAsync(new BlockchainWalletsBackupIsPrimaryChangesIndex
            {
                Address = address,
                Version = version,
                PartitionKey = BlockchainWalletsBackupIsPrimaryChangesIndex.GetPartitionKey(clientId, blockchainType),
                RowKey = BlockchainWalletsBackupIsPrimaryChangesIndex.GetRowKey(version)
            }, replaceCondition: p => false);
        }

        public async Task DeleteIfExistAsync(string blockchainType, Guid clientId, string address)
        {
            await _isDeletedStorage.InsertOrReplaceAsync(new BlockchainWalletsArchiveIndex
            {
                PartitionKey = BlockchainWalletsArchiveIndex.GetPartitionKey(clientId),
                RowKey = BlockchainWalletsArchiveIndex.GetRowKey(address, blockchainType)
            });
        }

        private async Task<bool> IsPrimaryWallet(Guid clientId, string blockchainType, string address)
        {
            var lastPrimary = await _isPrimaryIndexStorage.GetTopRecordAsync(
                BlockchainWalletsBackupIsPrimaryChangesIndex.GetPartitionKey(clientId, blockchainType));

            return lastPrimary?.Address == address;
        }

        private async Task<bool> IsDeleted(Guid clientId, string address, string blockchainType)
        {
            return await _isDeletedStorage.GetDataAsync(BlockchainWalletsArchiveIndex.GetPartitionKey(clientId),
                       BlockchainWalletsArchiveIndex.GetRowKey(address, blockchainType)) != null;
        }
    }
}
