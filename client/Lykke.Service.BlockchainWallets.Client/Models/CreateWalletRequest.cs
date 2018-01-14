using System;

namespace Lykke.Service.BlockchainWallets.Client.Models
{
    /// <summary>
    ///    Wallet creation request.
    /// </summary>
    public class CreateWalletRequest
    {
        /// <summary>
        ///    Lykke client id.
        /// </summary>
        public Guid ClientId { get; set; }
    }
}
