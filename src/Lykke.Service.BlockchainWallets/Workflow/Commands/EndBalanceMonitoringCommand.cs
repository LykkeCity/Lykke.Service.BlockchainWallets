using MessagePack;

namespace Lykke.Service.BlockchainWallets.Workflow.Commands
{
    [MessagePackObject]
    public class EndBalanceMonitoringCommand
    {
        [Key(0)]
        public string Address { get; set; }

        [Key(1)]
        public string BlockchainType { get; set; }
    }
}
