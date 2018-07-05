using System;
using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.BlockchainWallets.Core.Settings
{
    [UsedImplicitly]
    public class AssetServiceClientSettings
    {
        [HttpCheck("api/isalive")]
        public string ServiceUrl { get; set; }
        public TimeSpan ExpirationPeriod { get; set; }
    }
}
