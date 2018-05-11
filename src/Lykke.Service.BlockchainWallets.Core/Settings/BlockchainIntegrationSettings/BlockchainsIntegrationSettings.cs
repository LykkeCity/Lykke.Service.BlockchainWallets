using System.Collections.Generic;
using JetBrains.Annotations;

namespace Lykke.Service.BlockchainWallets.Core.Settings.BlockchainIntegrationSettings
{
    [UsedImplicitly]
    public class BlockchainsIntegrationSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public IReadOnlyList<BlockchainSettings> Blockchains { get; set; }
    }
}
