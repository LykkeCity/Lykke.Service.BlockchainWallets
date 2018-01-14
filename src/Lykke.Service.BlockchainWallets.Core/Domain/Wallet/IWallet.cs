using System;

namespace Lykke.Service.BlockchainWallets.Core.Domain.Wallet
{
    public interface IWallet
    {
        string Address { get; set; }

        string AssetId { get; set; }

        string IntegrationLayerId { get; set; }

        Guid ClientId { get; set; }
    }
}
