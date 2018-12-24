using JetBrains.Annotations;

namespace Lykke.Service.BlockchainWallets.Core.Settings.ServiceSettings
{
    [UsedImplicitly]
    public class BlockchainWalletsSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public CqrsSettings Cqrs { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public DbSettings Db { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string SignFacadeApiKey { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public int BlockchainApiTimeoutInSeconds { get; set; }
    }
}
