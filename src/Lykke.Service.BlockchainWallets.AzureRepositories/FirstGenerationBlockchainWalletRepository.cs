using System;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.SettingsReader;

namespace Lykke.Service.BlockchainWallets.AzureRepositories
{
    public class FirstGenerationBlockchainWalletRepository : IFirstGenerationBlockchainWalletRepository
    {
        private readonly INoSQLTableStorage<FirstGenerationBlockchainWalletEntity.FromBcnClientCredentials> _bcnClientCredentialsWalletTable;
        private readonly INoSQLTableStorage<FirstGenerationBlockchainWalletEntity.FromWalletCredentials> _walletCredentialsWalletTable;

        
        private FirstGenerationBlockchainWalletRepository(
            INoSQLTableStorage<FirstGenerationBlockchainWalletEntity.FromBcnClientCredentials> bcnClientCredentialsWalletTable,
            INoSQLTableStorage<FirstGenerationBlockchainWalletEntity.FromWalletCredentials> walletCredentialsWalletTable)
        {
            _bcnClientCredentialsWalletTable = bcnClientCredentialsWalletTable;
            _walletCredentialsWalletTable = walletCredentialsWalletTable;
        }
        
        public static IFirstGenerationBlockchainWalletRepository Create(
            IReloadingManager<string> clientPersonalInfoConnectionString,
            ILog log)
        {
            const string bcnClientCredentialsTableName = "BcnClientCredentials";
            const string walletCredentialsTableName = "WalletCredentials";

            var bcnCredentialsWalletTable = AzureTableStorage<FirstGenerationBlockchainWalletEntity.FromBcnClientCredentials>.Create
            (
                clientPersonalInfoConnectionString,
                bcnClientCredentialsTableName,
                log
            );

            var walletCredentialsWalletTable = AzureTableStorage<FirstGenerationBlockchainWalletEntity.FromWalletCredentials>.Create
            (
                clientPersonalInfoConnectionString,
                walletCredentialsTableName,
                log
            );
            
            return new FirstGenerationBlockchainWalletRepository
            (
                bcnCredentialsWalletTable,
                walletCredentialsWalletTable
            );
        }


        private static (string PartitionKey, string RowKey) GetBcnClientCredentialsWalletKeys(string assetId, Guid clientId)
        {
            return (clientId.ToString(), assetId);
        }
        
        private static (string PartitionKey, string RowKey) GetWalletCredentialsWalletKeys(Guid clientId)
        {
            return ("Wallet", clientId.ToString());
        }
        
        public async Task<FirstGenerationBlockchainWalletDto> TryGetAsync(string assetId, Guid clientId)
        {
            if (assetId == SpecialAssetIds.Bitcoin || assetId == SpecialAssetIds.Ethereum)
            {
                var (partitionKey, rowKey) = GetBcnClientCredentialsWalletKeys(assetId, clientId);
                var entity = await _bcnClientCredentialsWalletTable.GetDataAsync(partitionKey, rowKey);

                if (entity != null)
                {
                    return new FirstGenerationBlockchainWalletDto
                    {
                        Address = entity.Address,
                        AssetId = entity.AssetId,
                        ClientId = Guid.Parse(entity.ClientId) 
                    };
                }                
            }
            else if (assetId == SpecialAssetIds.Solarcoin)
            {
                var (partitionKey, rowKey) = GetWalletCredentialsWalletKeys(clientId);
                var entity = await _walletCredentialsWalletTable.GetDataAsync(partitionKey, rowKey);

                if (!string.IsNullOrEmpty(entity?.SolarCoinWalletAddress))
                {
                    return new FirstGenerationBlockchainWalletDto
                    {
                        Address = entity.SolarCoinWalletAddress,
                        AssetId = assetId,
                        ClientId = Guid.Parse(entity.ClientId)
                    };
                }
            }
            
            return null;
        }
    }
}
