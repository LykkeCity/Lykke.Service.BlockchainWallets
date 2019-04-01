using System;

namespace Lykke.Service.BlockchainWallets.Core.DTOs.Validation
{
    public class CashoutModel
    {
        public string AssetId { get; set; }

        public decimal? Amount { get; set; }

        public Guid? ClientId { get; set; }

        public string DestinationAddress { get; set; }
    }
}
