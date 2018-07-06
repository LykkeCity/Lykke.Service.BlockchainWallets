using JetBrains.Annotations;
using Lykke.Service.BlockchainWallets.Core.Settings.BlockchainIntegrationSettings;
using Lykke.Service.BlockchainWallets.Core.Settings.BlockchainSignFacadeClient;
using Lykke.Service.BlockchainWallets.Core.Settings.ServiceSettings;
using Lykke.Service.BlockchainWallets.Core.Settings.SlackNotifications;

namespace Lykke.Service.BlockchainWallets.Core.Settings
{
    [UsedImplicitly]
    public class AppSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public BlockchainsIntegrationSettings BlockchainsIntegration { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public BlockchainSignFacadeClientSettings BlockchainSignFacadeClient { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public BlockchainWalletsSettings BlockchainWalletsService { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public SlackNotificationsSettings SlackNotifications { get; set; }
        
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public AssetServiceClientSettings AssetsServiceClient { get; set; }
    }
}
