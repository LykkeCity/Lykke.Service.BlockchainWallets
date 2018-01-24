using System;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using Common.Log;
using Lykke.Service.BlockchainWallets.Core.Domain.Wallet;
using Lykke.SettingsReader;


namespace Lykke.Service.BlockchainWallets.AzureRepositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly INoSQLTableStorage<AzureIndex>   _addressIndexTable;
        private readonly INoSQLTableStorage<WalletEntity> _walletsTable;

        private WalletRepository(
            INoSQLTableStorage<AzureIndex> addressIndexTable,
            INoSQLTableStorage<WalletEntity> walletsTable)
        {
            _addressIndexTable = addressIndexTable;
            _walletsTable      = walletsTable;
        }


        public async Task AddAsync(string integrationLayerId, string assetId, Guid clientId, string address)
        {
            var partitionKey = WalletEntity.GetPartitionKey(integrationLayerId, assetId);
            var rowKey       = WalletEntity.GetRowKey(clientId);

            // Address index

            (var indexPartitionKey, var indexRowKey) = GetAddressIndexKeys(integrationLayerId, assetId, address);

            await _addressIndexTable.InsertOrReplaceAsync(new AzureIndex(
                partitionKey:        indexPartitionKey,
                rowKey:              indexRowKey,
                primaryPartitionKey: partitionKey,
                primaryRowKey:       rowKey
            ));


            // Wallet entity
            
            await _walletsTable.InsertOrReplaceAsync(new WalletEntity
            {
                PartitionKey = partitionKey,
                RowKey       = rowKey,

                Address            = address,
                AssetId            = assetId,
                ClientId           = clientId,
                IntegrationLayerId = integrationLayerId
            });
        }

        public async Task DeleteIfExistsAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            var partitionKey = WalletEntity.GetPartitionKey(integrationLayerId, assetId);
            var rowKey       = WalletEntity.GetRowKey(clientId);

            var wallet = await _walletsTable.GetDataAsync(partitionKey, rowKey);
            
            if (wallet != null)
            {
                (var indexPartitionKey, var indexRowKey) = GetAddressIndexKeys(wallet);

                await _walletsTable.DeleteIfExistAsync(wallet.PartitionKey, wallet.RowKey);
                await _addressIndexTable.DeleteIfExistAsync(indexPartitionKey, indexRowKey);
            }
        }

        public async Task<bool> ExistsAsync(string integrationLayerId, string assetId, string address)
        {
            return (await TryGetAsync(integrationLayerId, assetId, address)) != null;
        }

        public async Task<bool> ExistsAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            return (await TryGetAsync(integrationLayerId, assetId, clientId)) != null;
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
            else
            {
                return null;
            }
        }

        public async Task<IWallet> TryGetAsync(string integrationLayerId, string assetId, Guid clientId)
        {
            var partitionKey = WalletEntity.GetPartitionKey(integrationLayerId, assetId);
            var rowKey       = WalletEntity.GetRowKey(clientId);

            return await _walletsTable.GetDataAsync(partitionKey, rowKey);
        }


        public static IWalletRepository Create(IReloadingManager<string> connectionString, ILog log)
        {
            const string tableName = "Wallets";

            var addressIndexTable = AzureTableStorage<AzureIndex>.Create
            (
                connectionStringManager: connectionString,
                tableName:               tableName,
                log:                     log
            );

            var walletsTable = AzureTableStorage<WalletEntity>.Create
            (
                connectionStringManager: connectionString,
                tableName:               tableName,
                log:                     log
            );

            return new WalletRepository(addressIndexTable, walletsTable);
        }

        private static (string PartitionKey, string RowKey) GetAddressIndexKeys(IWallet wallet)
        {
            return GetAddressIndexKeys(wallet.IntegrationLayerId, wallet.AssetId, wallet.Address);
        }

        private static (string PartitionKey, string RowKey) GetAddressIndexKeys(string integrationLayerId, string assetId, string address)
        {
            var partitionKey = $"AddressIndex-{WalletEntity.GetPartitionKey(integrationLayerId, assetId)}";
            var rowKey       = address;

            return (partitionKey, rowKey);
        }
    }
}
