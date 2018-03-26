using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.BlockchainWallets.Client.Models
{
    /// <summary>
    ///     Blockchain wallet.
    /// </summary>
    public class ClientWalletResponse
    {
        /// <summary>
        ///     Blockchain address.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        ///     Client Id.
        /// </summary>
        public Guid ClientId { get; set; }

        /// <summary>
        ///     Blockchain Integration Layer Id.
        /// </summary>
        public string IntegrationLayerId { get; set; }

        /// <summary>
        ///     Blockchain Integration Layer Asset Id.
        /// </summary>
        public string IntegrationLayerAssetId { get; set; }
    }
}
