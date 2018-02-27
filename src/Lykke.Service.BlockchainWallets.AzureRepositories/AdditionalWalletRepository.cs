using System;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using Common;
using Common.Log;
using Lykke.Service.BlockchainWallets.Core.Domain.Wallet;
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


        public static IAdditionalWalletRepository Create(IReloadingManager<string> connectionString, ILog log)
        {
            const string tableName = "AdditionalWallets";
            const string indexTableName = "AdditionalWalletsAddressIndex";

            var addressIndexTable = AzureTableStorage<AzureIndex>.Create
            (
                connectionString,
                indexTableName,
                log
            );

            var additionalWalletsTable = AzureTableStorage<AdditionalWalletEntity>.Create
            (
                connectionString,
                tableName,
                log
            );

            return new AdditionalWalletRepository(addressIndexTable, additionalWalletsTable);
        }


        private static (string PartitionKey, string RowKey) GetAddressIndexKeys(string integrationLayerId, string assetId, string address)
        {
            var partitionKey = $"{integrationLayerId}-{assetId}-{address.CalculateHexHash32(3)}";
            var rowKey = address;

            return (partitionKey, rowKey);
        }


        public async Task AddAsync(string integrationLayerId, string assetId, Guid clientId, string address)
        {
            var partitionKey = AdditionalWalletEntity.GetPartitionKey(integrationLayerId, assetId, clientId);
            var rowKey = AdditionalWalletEntity.GetRowKey(address);

            // Address index

            (var indexPartitionKey, var indexRowKey) = GetAddressIndexKeys(integrationLayerId, assetId, address);
            
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
                IntegrationLayerId = integrationLayerId
            });
        }

        public async Task DeleteAllAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            var partitionKey = AdditionalWalletEntity.GetPartitionKey(integrationLayerId, assetId, clientId);
            var wallets = await _additionalWalletsTable.GetDataAsync(partitionKey);

            foreach (var wallet in wallets)
            {
                (var indexPartitionKey, _) = GetAddressIndexKeys(wallet.IntegrationLayerId, wallet.AssetId, wallet.Address);

                await _additionalWalletsTable.DeleteIfExistAsync(wallet.PartitionKey, wallet.RowKey);
                await _addressIndexTable.DeleteIfExistAsync(indexPartitionKey, wallet.Address);
            }
        }

        public async Task<bool> ExistsAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            var partitionKey = AdditionalWalletEntity.GetPartitionKey(integrationLayerId, assetId, clientId);
            
            return (await _additionalWalletsTable.GetDataAsync(partitionKey)).Any();
        }

        public async Task<IWallet> TryGetAsync(string integrationLayerId, string assetId, string address)
        {
            (var indexPartitionKey, _) = GetAddressIndexKeys(integrationLayerId, assetId, address);

            var index = (await _addressIndexTable.GetDataAsync(indexPartitionKey)).FirstOrDefault();

            if (index != null)
            {
                var wallet = await _additionalWalletsTable.GetDataAsync(index.PrimaryPartitionKey, index.PrimaryRowKey);

                return wallet;
            }

            return null;
        }
    }
}
