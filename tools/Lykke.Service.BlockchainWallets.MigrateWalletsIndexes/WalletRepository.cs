using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using Common;
using Lykke.Common.Log;
using Lykke.SettingsReader;
using MoreLinq;

namespace Lykke.Service.BlockchainWallets.MigrateWalletsIndexes
{
    namespace Lykke.Service.BlockchainWallets.AzureRepositories
    {
        public class WalletRepository
        {
            private readonly INoSQLTableStorage<AzureIndex> _addressIndexTable;
            private readonly INoSQLTableStorage<WalletEntity> _walletsTable;

            private WalletRepository(
                INoSQLTableStorage<AzureIndex> addressIndexTable,
                INoSQLTableStorage<WalletEntity> walletsTable)
            {
                _addressIndexTable = addressIndexTable;
                _walletsTable = walletsTable;
            }


            public static WalletRepository Create(IReloadingManager<string> connectionString, ILogFactory logFactory)
            {
                const string tableName = "Wallets";
                const string indexTableName = "WalletsAddressIndex";

                var addressIndexTable = AzureTableStorage<AzureIndex>.Create
                (
                    connectionString,
                    indexTableName,
                    logFactory
                );

                var walletsTable = AzureTableStorage<WalletEntity>.Create
                (
                    connectionString,
                    tableName,
                    logFactory
                );

                return new WalletRepository(addressIndexTable, walletsTable);
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

            #endregion


            public async Task<(IEnumerable<WalletEntity> Wallets, string ContinuationToken)> GetAsync(int take, string continuationToken)
            {
                IEnumerable<WalletEntity> entities;

                (entities, continuationToken) = await _walletsTable.GetDataWithContinuationTokenAsync(take, continuationToken);

                return (entities, continuationToken);
            }


            public async Task DeleteAllAddressIndexesAsync()
            {
                string continuation = null;
                do
                {
                    IEnumerable<AzureIndex> indexes;
                    (indexes, continuation) = await _addressIndexTable.GetDataWithContinuationTokenAsync(100, continuation);

                    foreach (var batch in indexes.Batch(10))
                        await Task.WhenAll(batch.Select(o => _addressIndexTable.DeleteAsync(o)));
                } while (continuation != null);
            }

            public Task AddAddressIndex(WalletEntity entity)
            {
                var (indexPartitionKey, indexRowKey) = GetAddressIndexKeys(entity);
                return _addressIndexTable.InsertOrReplaceAsync(new AzureIndex(
                    indexPartitionKey,
                    indexRowKey,
                    entity.PartitionKey,
                    entity.RowKey
                ));
            }
        }
    }

}
