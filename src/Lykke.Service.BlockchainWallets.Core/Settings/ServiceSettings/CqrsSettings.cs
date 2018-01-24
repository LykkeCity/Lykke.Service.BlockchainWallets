using System;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.BlockchainWallets.Core.Settings.ServiceSettings
{
    public class CqrsSettings
    {
        [AmqpCheck]
        public string RabbitConnectionString { get; set; }

        public TimeSpan RetryDelay { get; set; }
    }
}
