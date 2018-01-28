using Lykke.Service.BlockchainWallets.Core.Settings.BlockchainIntegrationSettings;
using Lykke.Service.BlockchainWallets.Core.Settings.ServiceSettings;
using Lykke.Service.BlockchainWallets.Core.Settings.SlackNotifications;

namespace Lykke.Service.BlockchainWallets.Core.Settings
{
    public class AppSettings
    {
        public BlockchainsIntegrationSettings BlockchainsIntegration { get; set; }

        public BlockchainWalletsSettings BlockchainWalletsService { get; set; }

        public SlackNotificationsSettings SlackNotifications { get; set; }
    }
}
