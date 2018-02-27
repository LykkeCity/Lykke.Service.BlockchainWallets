using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.BlockchainWallets.Core.Settings.ServiceSettings
{
    public class BlockchainWalletsSettings
    {
        [Optional]
        public bool AllowConversion { get; set; }

        public CqrsSettings Cqrs { get; set; }

        public DbSettings Db { get; set; }
    }
}
