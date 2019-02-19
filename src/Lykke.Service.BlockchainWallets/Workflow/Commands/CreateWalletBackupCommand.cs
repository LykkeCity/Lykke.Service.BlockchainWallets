using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.BlockchainWallets.Contract;
using MessagePack;

namespace Lykke.Service.BlockchainWallets.Workflow.Commands
{
    public class CreateWalletBackupCommand
    {
        [Key(0)]
        public string Address { get; set; }

        [Key(1)]
        public string AssetId { get; set; }

        [Key(3)]
        public string BlockchainType { get; set; }

        [Key(4)]
        public Guid ClientId { get; set; }

        [Key(5)]
        public CreatorType CreatedBy { get; set; }

        [Key(6)]
        public bool IsPrimary { get; set; }
    }
}
