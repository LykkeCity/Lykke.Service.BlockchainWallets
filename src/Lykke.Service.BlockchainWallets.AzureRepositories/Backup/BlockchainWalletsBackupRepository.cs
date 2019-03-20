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
        private readonly INoSQLTableStorage<BlockchainWalletsBackupIsPrimaryIndex> _isPrimaryIndexStorage;
        private readonly INoSQLTableStorage<BlockchainWalletsArchiveIndex> _isDeletedStorage;

        private BlockchainWalletsBackupRepository(INoSQLTableStorage<BlockchainWalletBackupEntity> storage,
            INoSQLTableStorage<BlockchainWalletsBackupIsPrimaryIndex> isPrimaryStorage,
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
                "BlockchainWalletsBackupV2",
                logFactory
            ), AzureTableStorage<BlockchainWalletsBackupIsPrimaryIndex>.Create
            (
                connectionString,
                "BlockchainWalletsBackupV2IsPrimaryIndex",
                logFactory
            ), AzureTableStorage<BlockchainWalletsArchiveIndex>.Create
            (
                connectionString,
                "BlockchainWalletsBackupV2ArchiveIndex",
                logFactory
            ));
        }

        public async Task AddAsync(string integrationLayerId, Guid clientId, string address, CreatorType createdBy, bool isPrimary)
        {
            await _storage.InsertOrReplaceAsync(new BlockchainWalletBackupEntity
            {
                ClientId = clientId,
                Address = address,
                PartitionKey = BlockchainWalletBackupEntity.GetPartitionKey(clientId),
                RowKey = BlockchainWalletBackupEntity.GetRowKey(address, integrationLayerId),
                CreatedBy = createdBy,
                IntegrationLayerId = integrationLayerId,
            });

            if (isPrimary)
            {
                await _isPrimaryIndexStorage.InsertOrReplaceAsync(new BlockchainWalletsBackupIsPrimaryIndex
                {
                    PartitionKey = BlockchainWalletsBackupIsPrimaryIndex.GetPartitionKey(clientId),
                    RowKey = BlockchainWalletsBackupIsPrimaryIndex.GetRowKey(integrationLayerId),
                    Address = address
                });
            }
        }

        public async Task<(IReadOnlyCollection<(string integrationLayerId, Guid clientId, string address, CreatorType createdBy, bool isPrimary)> Entities, string ContinuationToken)>
            GetDataWithContinuationTokenAsync(int take, string continuationToken)
        {
            var queryResult = await _storage.GetDataWithContinuationTokenAsync(take, continuationToken);

            var mapped = await queryResult.Entities.SelectAsync(async p => (integrationLayerId: p.IntegrationLayerId,
                clientId: p.ClientId,
                address: p.Address,
                createdBy: p.CreatedBy,
                isPrimary: await IsPrimaryWallet(p.ClientId, p.IntegrationLayerId, p.Address),
                IsDeleted: await IsDeleted(p.ClientId, p.Address, p.IntegrationLayerId )));

            return (mapped.Where(p => !p.IsDeleted).Select(p=> (p.integrationLayerId, p.clientId, p.address, p.createdBy, p.isPrimary)).ToList(), 
                queryResult.ContinuationToken);
        }

        public async Task DeleteIfExistAsync(string integrationLayerId, Guid clientId, string address)
        {
            if (await IsPrimaryWallet(clientId, integrationLayerId, address))
            {
                var allClientWallets = (await _storage.GetDataAsync(BlockchainWalletBackupEntity.GetPartitionKey(clientId)))
                    .Where(p => p.IntegrationLayerId == integrationLayerId && p.Address != address)
                    .OrderByDescending(p => p.Timestamp.UtcDateTime);

                BlockchainWalletBackupEntity nextWallet = null;
                foreach (var wallet in allClientWallets)
                {
                    if (!await IsDeleted(wallet.ClientId, wallet.Address, wallet.IntegrationLayerId))
                    {
                        nextWallet = wallet;
                    }
                    break;
                }

                if (nextWallet != null)
                {
                    await _isPrimaryIndexStorage.InsertOrReplaceAsync(new BlockchainWalletsBackupIsPrimaryIndex
                    {
                        PartitionKey = BlockchainWalletsBackupIsPrimaryIndex.GetPartitionKey(clientId),
                        RowKey = BlockchainWalletsBackupIsPrimaryIndex.GetRowKey(integrationLayerId),
                        Address = nextWallet.Address
                    });
                }

                await _isPrimaryIndexStorage.DeleteIfExistAsync(BlockchainWalletsBackupIsPrimaryIndex.GetPartitionKey(clientId),
                    BlockchainWalletsBackupIsPrimaryIndex.GetRowKey(integrationLayerId),
                    p => p.Address == address);
            }

            await _isDeletedStorage.InsertOrReplaceAsync(new BlockchainWalletsArchiveIndex
            {
                PartitionKey = BlockchainWalletsArchiveIndex.GetPartitionKey(clientId),
                RowKey = BlockchainWalletsArchiveIndex.GetRowKey(address, integrationLayerId)
            });
        }

        private async Task<bool> IsPrimaryWallet(Guid clientId, string integrationLayerId, string address)
        {
            return (await _isPrimaryIndexStorage.GetDataAsync(BlockchainWalletsBackupIsPrimaryIndex.GetPartitionKey(clientId),
                       BlockchainWalletsBackupIsPrimaryIndex.GetRowKey(integrationLayerId)))?.Address == address;
        }

        private async Task<bool> IsDeleted(Guid clientId, string address, string integrationLayerId)
        {
            return await _isDeletedStorage.GetDataAsync(BlockchainWalletsArchiveIndex.GetPartitionKey(clientId),
                       BlockchainWalletsArchiveIndex.GetRowKey(address, integrationLayerId)) != null;
        }
    }
}
