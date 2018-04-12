using System;
using JetBrains.Annotations;

namespace Lykke.Service.BlockchainWallets.Models
{
    /// <summary>
    ///     Blockchain wallet.
    /// </summary>
    public class WalletResponse
    {
        /// <summary>
        ///     Blockchain address.
        /// </summary>
        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        public string Address { get; set; }

        /// <summary>
        ///     Blockchain address extension.
        /// </summary>
        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        public string AddressExtension { get; set; }

        /// <summary>
        ///     Blockchain Integration Layer Id.
        /// </summary>
        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        public string BlockchainType { get; set; }

        /// <summary>
        ///     Client Id.
        /// </summary>
        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        public Guid ClientId { get; set; }

        /// <summary>
        ///     Blockchain Integration Layer Id.
        /// </summary>
        [UsedImplicitly(ImplicitUseKindFlags.Access), Obsolete]
        public string IntegrationLayerId { get; set; }

        /// <summary>
        ///     Blockchain Integration Layer Asset Id.
        /// </summary>
        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        public string IntegrationLayerAssetId { get; set; }
    }
}
