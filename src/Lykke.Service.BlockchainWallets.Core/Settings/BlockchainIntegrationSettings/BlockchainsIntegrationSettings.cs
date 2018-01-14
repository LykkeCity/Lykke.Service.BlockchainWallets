using System.Collections.Generic;


namespace Lykke.Service.BlockchainWallets.Core.Settings.BlockchainIntegrationSettings
{
    public class BlockchainsIntegrationSettings
    {
        public IReadOnlyList<BlockchainSettings> Blockchains { get; set; }
    }
}
