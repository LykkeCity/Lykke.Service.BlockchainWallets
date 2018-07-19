using System;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Service.BlockchainWallets.AzureRepositories.FirstGeneration;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.SettingsReader;

namespace Lykke.Service.BlockchainWallets.AzureRepositories
{
    public class FirstGenerationBlockchainWalletRepository : IFirstGenerationBlockchainWalletRepository
    {
        private readonly INoSQLTableStorage<FirstGenerationBlockchainWalletEntity.FromBcnClientCredentials>
            _bcnClientCredentialsWalletTable;

        private readonly INoSQLTableStorage<FirstGenerationBlockchainWalletEntity.FromWalletCredentials>
            _walletCredentialsWalletTable;


        private FirstGenerationBlockchainWalletRepository(
            INoSQLTableStorage<FirstGenerationBlockchainWalletEntity.FromBcnClientCredentials>
                bcnClientCredentialsWalletTable,
            INoSQLTableStorage<FirstGenerationBlockchainWalletEntity.FromWalletCredentials>
                walletCredentialsWalletTable)
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

            var bcnCredentialsWalletTable =
                AzureTableStorage<FirstGenerationBlockchainWalletEntity.FromBcnClientCredentials>.Create
                (
                    clientPersonalInfoConnectionString,
                    bcnClientCredentialsTableName,
                    log
                );

            var walletCredentialsWalletTable =
                AzureTableStorage<FirstGenerationBlockchainWalletEntity.FromWalletCredentials>.Create
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

        private static (string PartitionKey, string RowKey) GetBcnClientCredentialsWalletKeys(string assetId,
            Guid clientId)
        {
            return (clientId.ToString(), assetId);
        }

        private static (string PartitionKey, string RowKey) GetWalletCredentialsWalletKeys(Guid clientId)
        {
            return ("Wallet", clientId.ToString());
        }

        public async Task<IWalletCredentials> GetAsync(Guid clientId)
        {
            var (partitionKeyByClient, rowKeyByClient) = GetWalletCredentialsWalletKeys(clientId);

            var walletCredentials =
                await _walletCredentialsWalletTable.GetDataAsync(partitionKeyByClient, rowKeyByClient);

            return walletCredentials;
        }

        public async Task SetSolarCoinWallet(Guid clientId, string address)
        {
            //var walletCredentials = await GetAsync(clientId);

            //if (string.IsNullOrEmpty(walletCredentials.SolarCoinWalletAddress))
            //{
            //    walletCredentials.SolarCoinWalletAddress = address;
            //    await MergeAsync(walletCredentials);
            //}

            await SaveAsync(BcnCredentialsRecord.Create(SpecialAssetIds.SolarAssetId, clientId.ToString(), null, address, null));
        }

        public async Task SetChronoBankContract(Guid clientId, string contract)
        {
            var walletCredentials = await GetAsync(clientId);

            if (string.IsNullOrEmpty(walletCredentials.ChronoBankContract))
            {
                var changedRecord = WalletCredentials.Create(walletCredentials);
                changedRecord.ChronoBankContract = contract;
                await MergeAsync(changedRecord);
            }
        }

        public async Task<IBcnCredentialsRecord> GetBcnCredsAsync(string assetId, Guid clientId)
        {
            var bcnKeys = GetBcnClientCredentialsWalletKeys(assetId, clientId);
            var bcnEntity = await _bcnClientCredentialsWalletTable.GetDataAsync(bcnKeys.PartitionKey, bcnKeys.RowKey);

            return bcnEntity;
        }

        public async Task<FirstGenerationBlockchainWalletDto> TryGetAsync(string assetId, Guid clientId, bool isErc20,
            bool isEtherium)
        {
            FirstGenerationBlockchainWalletEntity.FromBcnClientCredentials bcnEntity = null;

            if (isErc20)
            {
                var bcn223Keys = GetBcnClientCredentialsWalletKeys(SpecialAssetIds.BcnKeyForErc223, clientId);
                var bcn20Keys = GetBcnClientCredentialsWalletKeys(SpecialAssetIds.BcnKeyForErc20, clientId);
                bcnEntity = await _bcnClientCredentialsWalletTable.GetDataAsync(bcn223Keys.PartitionKey, bcn223Keys.RowKey); 
            }
            else
            {
                var bcnKeys = GetBcnClientCredentialsWalletKeys(assetId, clientId);
                bcnEntity = await _bcnClientCredentialsWalletTable.GetDataAsync(bcnKeys.PartitionKey, bcnKeys.RowKey);
            }

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
                default:
                    address = null;
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

        public async Task SetQuantaContract(Guid clientId, string contract)
        {
            var walletCredentials = await GetAsync(clientId);

            if (string.IsNullOrEmpty(walletCredentials.QuantaContract))
            {
                var changedRecord = WalletCredentials.Create(walletCredentials);
                changedRecord.QuantaContract = contract;
                await MergeAsync(changedRecord);
            }
        }

        public async Task SaveAsync(IBcnCredentialsRecord credsRecord)
        {
            var byClientEntity = FirstGenerationBlockchainWalletEntity.FromBcnClientCredentials.ByClientId.Create(credsRecord);
            var byAssetAddressEntity = FirstGenerationBlockchainWalletEntity.FromBcnClientCredentials.ByAssetAddress.Create(credsRecord);

            await _bcnClientCredentialsWalletTable.TryInsertAsync(byClientEntity);
            await _bcnClientCredentialsWalletTable.TryInsertAsync(byAssetAddressEntity);
        }

        public Task SaveAsync(IWalletCredentials walletCredentials)
        {
            var newByClientEntity = WalletCredentialsEntity.ByClientId.CreateNew(walletCredentials);
            var newByMultisigEntity = WalletCredentialsEntity.ByMultisig.CreateNew(walletCredentials);
            var newByColoredEntity = WalletCredentialsEntity.ByColoredMultisig.CreateNew(walletCredentials);

            var insertByEthTask = Task.CompletedTask;
            if (!string.IsNullOrEmpty(walletCredentials.EthConversionWalletAddress))
            {
                var newByEthWalletEntity = WalletCredentialsEntity.ByEthContract.CreateNew(walletCredentials);
                insertByEthTask = _walletCredentialsWalletTable.InsertAsync(newByEthWalletEntity);
            }

            var insertBySolarTask = Task.CompletedTask;
            if (!string.IsNullOrEmpty(walletCredentials.SolarCoinWalletAddress))
            {
                var newBySolarWalletEntity = WalletCredentialsEntity.BySolarCoinWallet.CreateNew(walletCredentials);
                insertBySolarTask = _walletCredentialsWalletTable.InsertAsync(newBySolarWalletEntity);
            }

            var insertByChronoBankTask = Task.CompletedTask;
            if (!string.IsNullOrEmpty(walletCredentials.ChronoBankContract))
            {
                var newByChronoContractEntity = WalletCredentialsEntity.ByChronoBankContract.CreateNew(walletCredentials);
                insertByChronoBankTask = _walletCredentialsWalletTable.InsertAsync(newByChronoContractEntity);
            }

            var insertByQuantaTask = Task.CompletedTask;
            if (!string.IsNullOrEmpty(walletCredentials.QuantaContract))
            {
                var newByQuantaContractEntity = WalletCredentialsEntity.ByQuantaContract.CreateNew(walletCredentials);
                insertByQuantaTask = _walletCredentialsWalletTable.InsertAsync(newByQuantaContractEntity);
            }

            var insertMultisigTask = newByMultisigEntity.MultiSig != null ? 
                _walletCredentialsWalletTable.InsertAsync(newByMultisigEntity) : Task.CompletedTask;

            return Task.WhenAll(
                _walletCredentialsWalletTable.InsertAsync(newByClientEntity),
                insertMultisigTask,
                _walletCredentialsWalletTable.InsertAsync(newByColoredEntity),
                insertByEthTask,
                insertBySolarTask,
                insertByChronoBankTask,
                insertByQuantaTask
                );
        }

        /// <summary>
        /// Tokens to support:
        ///
        ///BTC
        ///Colored coins - LKK, LKK1y, CHF|USD|EUR|GBP - can be turned on in asset settings
        ///ETH
        ///ERC20 - PKT, LKK2y
        ///ERC223 - DEB
        ///Tree
        ///TIME
        ///SLR
        /// </summary>
        public async Task TryAddAsync(string assetId, Guid clientId, bool isErc20, bool isEtherium)
        {
            #region BTC & ColoredCoins LKK, LKK1y, CHF|USD|EUR|GBP

            #endregion

            #region ETH & ERC20/223

            #endregion

            #region Tree & Time & SLR

            #endregion
        }

        public Task MergeAsync(IWalletCredentials walletCredentials)
        {
            var newByClientEntity = WalletCredentialsEntity.ByClientId.CreateNew(walletCredentials);
            var newByMultisigEntity = WalletCredentialsEntity.ByMultisig.CreateNew(walletCredentials);
            var newByColoredEntity = WalletCredentialsEntity.ByColoredMultisig.CreateNew(walletCredentials);

            var insertByEthTask = Task.CompletedTask;
            if (!string.IsNullOrEmpty(walletCredentials.EthConversionWalletAddress))
            {
                var newByEthWalletEntity = WalletCredentialsEntity.ByEthContract.CreateNew(walletCredentials);
                insertByEthTask = _walletCredentialsWalletTable.InsertOrMergeAsync(newByEthWalletEntity);
            }

            var insertBySolarTask = Task.CompletedTask;
            if (!string.IsNullOrEmpty(walletCredentials.SolarCoinWalletAddress))
            {
                var newBySolarWalletEntity = WalletCredentialsEntity.BySolarCoinWallet.CreateNew(walletCredentials);
                insertBySolarTask = _walletCredentialsWalletTable.InsertOrMergeAsync(newBySolarWalletEntity);
            }

            var insertByChronoBankTask = Task.CompletedTask;
            if (!string.IsNullOrEmpty(walletCredentials.ChronoBankContract))
            {
                var newByChronoContractEntity = WalletCredentialsEntity.ByChronoBankContract.CreateNew(walletCredentials);
                insertByChronoBankTask = _walletCredentialsWalletTable.InsertOrMergeAsync(newByChronoContractEntity);
            }

            var insertByQuantaTask = Task.CompletedTask;
            if (!string.IsNullOrEmpty(walletCredentials.QuantaContract))
            {
                var newByQuantaEntity = WalletCredentialsEntity.ByQuantaContract.CreateNew(walletCredentials);
                insertByQuantaTask = _walletCredentialsWalletTable.InsertOrMergeAsync(newByQuantaEntity);
            }

            var insertMultisigTask = newByMultisigEntity.MultiSig != null ?
                _walletCredentialsWalletTable.InsertOrMergeAsync(newByMultisigEntity) : Task.CompletedTask;

            return Task.WhenAll(
                _walletCredentialsWalletTable.InsertOrMergeAsync(newByClientEntity),
                insertMultisigTask,
                _walletCredentialsWalletTable.InsertOrMergeAsync(newByColoredEntity),
                insertByEthTask,
                insertBySolarTask,
                insertByChronoBankTask,
                insertByQuantaTask);
        }
    }
}
