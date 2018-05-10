using MessagePack;

namespace Lykke.Service.BlockchainWallets.Workflow.Commands
{
    [MessagePackObject]
    public class BeginTransactionHistoryMonitoringCommand
    {
        [Key(0)]
        public string Address { get; set; }

        [Key(1)]
        public string AssetId { get; set; }

        [Key(2)]
        public string BlockchainType { get; set; }
    }
}
