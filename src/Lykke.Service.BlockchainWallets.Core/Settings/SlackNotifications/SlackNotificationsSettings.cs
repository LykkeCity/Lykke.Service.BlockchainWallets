using JetBrains.Annotations;

namespace Lykke.Service.BlockchainWallets.Core.Settings.SlackNotifications
{
    [UsedImplicitly]
    public class SlackNotificationsSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public AzureQueuePublicationSettings AzureQueue { get; set; }
    }
}
