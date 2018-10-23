using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.BlockchainWallets.BtcDepositsMigration
{
    public class BitcoinSettings
    {
        [HttpCheck("/api/isalive")] public string SignatureProviderUrl { get; set; }

        public string SigningServiceApiKey { get; set; }
    }

    internal class BitcoinAppSettings
    {
        public BitcoinSettings BitcoinService { get; set; }
    }
}
