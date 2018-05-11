using System;
using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.BlockchainWallets.Core.Settings.ServiceSettings
{
    [UsedImplicitly]
    public class CqrsSettings
    {
        [AmqpCheck]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string RabbitConnectionString { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public TimeSpan RetryDelay { get; set; }
    }
}
