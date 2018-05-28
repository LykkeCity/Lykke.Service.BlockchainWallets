using System;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.SettingsReader;

namespace Lykke.Service.BlockchainWallets.AzureRepositories
{
    public class BcnCredentialsWalletRepository : IBcnCredentialsWalletRepository
    {
        private readonly INoSQLTableStorage<BcnCredentialsWalletEntity> _table;

        
        private BcnCredentialsWalletRepository(
            INoSQLTableStorage<BcnCredentialsWalletEntity> table)
        {
            _table = table;
        }
        
        public static IBcnCredentialsWalletRepository Create(IReloadingManager<string> connectionString, ILog log)
        {
            const string tableName = "BcnClientCredentials";

            var additionalWalletsTable = AzureTableStorage<BcnCredentialsWalletEntity>.Create
            (
                connectionString,
                tableName,
                log
            );

            return new BcnCredentialsWalletRepository(additionalWalletsTable);
        }


        private static (string PartitionKey, string RowKey) GetKeys(string assetId, Guid clientId)
        {
            return (clientId.ToString(), assetId);
        }
        
        public async Task<BcnCredentialsWalletDto> TryGetAsync(string assetId, Guid clientId)
        {
            var (partitionKey, rowKey) = GetKeys(assetId, clientId);
            var entity = await _table.GetDataAsync(partitionKey, rowKey);

            if (entity != null)
            {
                return new BcnCredentialsWalletDto
                {
                    Address = entity.Address,
                    AssetId = entity.AssetId,
                    ClientId = Guid.Parse(entity.ClientId) 
                };
            }

            return null;
        }
    }
}
