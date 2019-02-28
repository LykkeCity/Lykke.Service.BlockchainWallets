using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common;
using Lykke.Common.Log;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.BlockchainWallets.BchDepositsMigration.ObservableWallet
{
    public class ObservableWalletEntity : TableEntity, IObservableWallet
    {
        public string Address { get; set; }

        public static string GeneratePartitionKey(string address)
        {
            return address.CalculateHexHash32(3);
        }

        public static string GenerateRowKey(string address)
        {
            return address;
        }

        public static ObservableWalletEntity Create(IObservableWallet source)
        {
            return new ObservableWalletEntity
            {
                Address = source.Address,
                PartitionKey = GeneratePartitionKey(source.Address),
                RowKey = GenerateRowKey(source.Address)
            };
        }
    }
    public class ObservableWalletRepository 
    {
        private readonly INoSQLTableStorage<ObservableWalletEntity> _storage;


        public static ObservableWalletRepository Create(IReloadingManager<string> connString, ILogFactory logFactory)
        {
            return new ObservableWalletRepository(AzureTableStorage<ObservableWalletEntity>.Create(connString, "ObservableWallets", logFactory));
        }

        public ObservableWalletRepository(INoSQLTableStorage<ObservableWalletEntity> storage)
        {
            _storage = storage;
        }


        public async Task<IEnumerable<IObservableWallet>> GetAll()
        {
            return await _storage.GetDataAsync();
        }
    }
}
