using ProtoBuf;

namespace Lykke.Service.BlockchainWallets.Core.Domain.Wallet.Commands
{
    [ProtoContract]
    public class BeginBalanceMonitoringCommand
    {
        [ProtoMember(1)]
        public string Address { get; set; }

        [ProtoMember(2)]
        public string AssetId { get; set; }

        [ProtoMember(3)]
        public string IntegrationLayerId { get; set; }
    }
}
