using System;
using MessagePack;

namespace Lykke.Service.BlockchainWallets.Workflow.Commands
{
    [MessagePackObject]
    public class SetPrimaryWalletBackupCommand
    {
        [Key(0)]
        public string Address { get; set; }

        [Key(1)]
        public string BlockchainType { get; set; }

        [Key(2)]
        public Guid ClientId { get; set; }

        [Key(3)]
        public int Version { get; set; }
    }
}
