using System;

namespace Lykke.Service.BlockchainWallets.Core.DTOs
{
    public class WalletDto
    {
        public string Address { get; set; }

        public string AssetId { get; set; }

        public string BlockchainType { get; set; }

        public Guid ClientId { get; set; }
    }
}
