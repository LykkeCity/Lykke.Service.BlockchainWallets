using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.BlockchainWallets.Core.Settings.BlockchainIntegrationSettings
{
    [UsedImplicitly]
    public class BlockchainSettings
    {
        [HttpCheck("/api/isalive")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string ApiUrl { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string HotWalletAddress { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string Type { get; set; }
    }
}
