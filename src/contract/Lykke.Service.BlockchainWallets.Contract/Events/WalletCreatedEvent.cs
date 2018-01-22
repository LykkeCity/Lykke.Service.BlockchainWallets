using MessagePack;

namespace Lykke.Service.BlockchainWallets.Contract.Events
{
    [MessagePackObject]
    public class WalletCreatedEvent
    {
        [Key(0)]
        public string Address { get; set; }

        [Key(1)]
        public string AssetId { get; set; }

        [Key(2)]
        public string IntegrationLayerId { get; set; }
    }
}
