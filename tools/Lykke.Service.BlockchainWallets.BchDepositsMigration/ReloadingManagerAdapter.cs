using System;
using System.Threading.Tasks;
using Lykke.SettingsReader;

namespace Lykke.Service.BlockchainWallets.BchDepositsMigration
{
    public class ReloadingManagerAdapter<T>:IReloadingManager<T>
    {
        private readonly T _value;

        public ReloadingManagerAdapter(T value)
        {
            _value = value;
        }

        public Task<T> Reload()
        {
            return Task.FromResult(_value);
        }

        public bool WasReloadedFrom(DateTime dateTime)
        {
            return false;
        }

        public bool HasLoaded => true;
        public T CurrentValue => _value;
    }
}
