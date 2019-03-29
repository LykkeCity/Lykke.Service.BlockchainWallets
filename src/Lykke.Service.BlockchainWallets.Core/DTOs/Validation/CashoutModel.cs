using System;

namespace Lykke.Service.BlockchainWallets.Core.DTOs.Validation
{
    public class CashoutModel
    {
        public string BlockchainType { get; set; }

        public string DestinationAddress { get; set; }
    }
}
