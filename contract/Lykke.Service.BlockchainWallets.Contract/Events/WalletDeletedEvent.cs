using System;
using JetBrains.Annotations;
using MessagePack;

namespace Lykke.Service.BlockchainWallets.Contract.Events
{
    [MessagePackObject, PublicAPI, Obsolete]
    public class WalletDeletedEvent
    {
        [Key(0)]
        public string Address { get; set; }

        [Key(1)]
        public string AssetId { get; set; }

        [Key(2), Obsolete("Use BlockchainType instead.")]
        public string IntegrationLayerId { get; set; }

        [Key(3)]
        public string BlockchainType { get; set; }

        [Key(4)]
        public Guid ClientId { get; set; }
    }
}
