using System;

namespace Lykke.Service.BlockchainWallets.Core.DTOs
{
    public class FirstGenerationBlockchainWalletDto
    {
        public string Address { get; set; }
        
        public string AssetId { get; set; }
        
        public Guid ClientId { get; set; }
    }
}
