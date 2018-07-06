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
        
        public async Task<FirstGenerationBlockchainWalletDto> TryGetAsync(string assetId, Guid clientId, bool isErc20, bool isEtherium)
        {
            string bcnRowKey = isErc20 ? SpecialAssetIds.BcnKeyForErc223 : assetId;
            var bcnKeys = GetBcnClientCredentialsWalletKeys(bcnRowKey, clientId);

            var bcnEntity = await _bcnClientCredentialsWalletTable.GetDataAsync(bcnKeys.PartitionKey, bcnKeys.RowKey);

            if (bcnEntity != null)
            {
                return new FirstGenerationBlockchainWalletDto
                {
                    Address = bcnEntity.AssetAddress,
                    AssetId = bcnEntity.AssetId,
                    ClientId = Guid.Parse(bcnEntity.ClientId) 
                };
            }

            if (isEtherium)
                return null;
            
            var (partitionKey, rowKey) = GetWalletCredentialsWalletKeys(clientId);
            var walletCredentials = await _walletCredentialsWalletTable.GetDataAsync(partitionKey, rowKey);

            if (walletCredentials == null)
                return null;

            string address;
            
            switch (assetId)
            {
                case SpecialAssetIds.BitcoinAssetId:
                    address = walletCredentials.MultiSig;
                    break;
                case SpecialAssetIds.SolarAssetId:
                    address = walletCredentials.SolarCoinWalletAddress;
                    break;
                case SpecialAssetIds.ChronoBankAssetId:
                    address = walletCredentials.ChronoBankContract;
                    break;
                case SpecialAssetIds.QuantaAssetId:
                    address = walletCredentials.QuantaContract;
                    break;
                default:
                    address = walletCredentials.ColoredMultiSig;
                    break;
            }

            return address == null
                ? null
                : new FirstGenerationBlockchainWalletDto
                {
                    Address = address,
                    AssetId = assetId,
                    ClientId = clientId
                };
        }
    }
}
