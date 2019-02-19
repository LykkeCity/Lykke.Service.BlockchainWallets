using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.BlockchainWallets.Core.Settings.ServiceSettings
{
    [UsedImplicitly]
    public class MongoConnectionSettings
    {
        [MongoCheck]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string ConnString { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string DbName { get; set; }

    }
}
