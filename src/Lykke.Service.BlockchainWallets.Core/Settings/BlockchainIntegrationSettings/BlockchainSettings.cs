using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.BlockchainWallets.Core.Settings.BlockchainIntegrationSettings
{
    public class BlockchainSettings
    {
        [HttpCheck("/api/isalive")]
        public string ApiUrl { get; set; }

        public string HotWalletAddress { get; set; }

        [HttpCheck("/api/isalive")]
        public string SignFacadeUrl { get; set; }

        public string Type { get; set; }
    }
}
