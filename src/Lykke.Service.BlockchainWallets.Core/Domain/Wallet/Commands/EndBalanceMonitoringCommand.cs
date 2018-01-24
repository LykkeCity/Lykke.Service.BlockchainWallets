using MessagePack;

namespace Lykke.Service.BlockchainWallets.Core.Domain.Wallet.Commands
{
    [MessagePackObject]
    public class EndBalanceMonitoringCommand
    {
        [Key(0)]
        public string Address { get; set; }

        [Key(1)]
        public string AssetId { get; set; }

        [Key(2)]
        public string IntegrationLayerId { get; set; }
    }
}
