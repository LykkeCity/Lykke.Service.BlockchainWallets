using System;
using Lykke.Service.BlockchainWallets.Contract;
using MessagePack;

namespace Lykke.Service.BlockchainWallets.Workflow.Commands
{
    public class DeleteWalletBackupCommand
    {
        [Key(0)]
        public string Address { get; set; }

        [Key(3)]
        public string BlockchainType { get; set; }

        [Key(4)]
        public Guid ClientId { get; set; }
    }
}
