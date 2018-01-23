using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.BlockchainWallets.Core.Settings.BlockchainIntegrationSettings
{
    public class BlockchainSettings
    {
        public string Type { get; set; }

        [HttpCheck("/api/isalive")]
        public string ApiUrl { get; set; }

        [HttpCheck("/api/isalive")]
        public string SignFacadeUrl { get; set; }

        public string HotWalletAddress { get; set; }
    }
}
