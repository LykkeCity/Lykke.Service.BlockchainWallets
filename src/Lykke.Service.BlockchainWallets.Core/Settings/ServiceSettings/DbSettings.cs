using JetBrains.Annotations;

namespace Lykke.Service.BlockchainWallets.Core.Settings.ServiceSettings
{
    [UsedImplicitly]
    public class DbSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string DataConnString { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string LogsConnString { get; set; }
    }
}
