using AzureStorage;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;
using Lykke.Service.BlockchainWallets.Core.Repositories.FirstGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.AzureRepositories.FirstGeneration
{
    public class WalletCredentialsHistoryRepository : IWalletCredentialsHistoryRepository
    {
        private readonly INoSQLTableStorage<WalletCredentialsHistoryRecord> _tableStorage;

        public WalletCredentialsHistoryRepository(INoSQLTableStorage<WalletCredentialsHistoryRecord> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        //container.RegisterInstance<IWalletCredentialsHistoryRepository>(
        //new WalletCredentialsHistoryRepository(
        //    AzureTableStorage<WalletCredentialsHistoryRecord>.Create(_dbSettings.ConnectionString(x => x.ClientPersonalInfoConnString),
        //"WalletCredentialsHistory", _log)));

        public async Task InsertHistoryRecord(IWalletCredentials oldWalletCredentials)
        {
            var entity = WalletCredentialsHistoryRecord.Create(oldWalletCredentials);
            await _tableStorage.InsertAndGenerateRowKeyAsDateTimeAsync(entity, DateTime.UtcNow);
        }

        public async Task<IEnumerable<string>> GetPrevMultisigsForUser(string clientId)
        {
            var prevWalletCreds =
                await _tableStorage.GetDataAsync(WalletCredentialsHistoryRecord.GeneratePartitionKey(clientId));

            return prevWalletCreds.Select(x => x.MultiSig);
        }
    }
}
