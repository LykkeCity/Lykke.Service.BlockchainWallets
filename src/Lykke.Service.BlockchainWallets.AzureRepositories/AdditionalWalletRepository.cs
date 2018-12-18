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

namespace Lykke.Service.BlockchainWallets.AzureRepositories
{
    public class AdditionalWalletRepository : IAdditionalWalletRepository
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


        public static IAdditionalWalletRepository Create(IReloadingManager<string> connectionString, ILogFactory logFactory)
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


        public async Task AddAsync(string blockchainType, string assetId, Guid clientId, string address)
        {
            var partitionKey = AdditionalWalletEntity.GetPartitionKey(blockchainType, assetId, clientId);
            var rowKey = AdditionalWalletEntity.GetRowKey(address);

            // Address index

            var (indexPartitionKey, indexRowKey) = GetAddressIndexKeys(blockchainType, address);
            
            await _addressIndexTable.InsertOrReplaceAsync(new AzureIndex(
                indexPartitionKey,
                indexRowKey,
                partitionKey,
                rowKey
            ));

            // Wallet entity

            await _additionalWalletsTable.InsertOrReplaceAsync(new AdditionalWalletEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,

                Address = address,
                AssetId = assetId,
                ClientId = clientId,
                IntegrationLayerId = blockchainType
            });
        }

        public async Task DeleteAllAsync(string blockchainType, string assetId, Guid clientId)
        {
            var partitionKey = AdditionalWalletEntity.GetPartitionKey(blockchainType, assetId, clientId);
            var wallets = await _additionalWalletsTable.GetDataAsync(partitionKey);

            foreach (var wallet in wallets)
            {
                var (indexPartitionKey, _) = GetAddressIndexKeys(wallet.IntegrationLayerId,  wallet.Address);

                await _additionalWalletsTable.DeleteIfExistAsync(wallet.PartitionKey, wallet.RowKey);
                await _addressIndexTable.DeleteIfExistAsync(indexPartitionKey, wallet.Address);
            }
        }

        public async Task<bool> ExistsAsync(string blockchainType, string assetId, Guid clientId)
        {
            var partitionKey = AdditionalWalletEntity.GetPartitionKey(blockchainType, assetId, clientId);
            
            return (await _additionalWalletsTable.GetDataAsync(partitionKey)).Any();
        }

        public async Task<WalletDto> TryGetAsync(string blockchainType, string address)
        {
            var  (indexPartitionKey, indexRowKey) = GetAddressIndexKeys(blockchainType, address);

            var index = await _addressIndexTable.GetDataAsync
            (
                partition: indexPartitionKey,
                row: indexRowKey
            );

            if (index != null)
            {
                var entity = await _additionalWalletsTable.GetDataAsync(index.PrimaryPartitionKey, index.PrimaryRowKey);

                return new WalletDto
                {
                    Address = entity.Address,
                    AssetId = entity.AssetId,
                    BlockchainType = entity.IntegrationLayerId,
                    ClientId = entity.ClientId
                };
            }

            return null;
        }

        public async Task<(IEnumerable<WalletDto> Wallets, string ContinuationToken)> GetAsync(int take, string continuationToken)
        {
            IEnumerable<AdditionalWalletEntity> entities;

            (entities, continuationToken) = await _additionalWalletsTable.GetDataWithContinuationTokenAsync(take, continuationToken);

            return (entities?.Select(entity => new WalletDto()
            {
                Address = entity.Address,
                AssetId = entity.AssetId,
                BlockchainType = entity.IntegrationLayerId,
                ClientId = entity.ClientId
            }), continuationToken);
        }
    }
}
