using System;
using Lykke.Service.BlockchainWallets.Contract;

namespace Lykke.Service.BlockchainWallets.Core.DTOs
{
    public class ChangedPrimaryWalletDto
    {
        public string Address { get; set; }
        
        public string BlockchainType { get; set; }

        public Guid ClientId { get; set; }

        public int Version { get; set; }
    }
}
