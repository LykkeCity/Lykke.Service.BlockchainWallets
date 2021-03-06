﻿using System;
using JetBrains.Annotations;
using MessagePack;

namespace Lykke.Service.BlockchainWallets.Contract.Events
{
    [MessagePackObject, PublicAPI]
    public class WalletArchivedEvent
    {
        [Key(0)]
        public string Address { get; set; }

        [Key(1)]
        public string BlockchainType { get; set; }

        [Key(2)]
        public Guid ClientId { get; set; }
    }
}
