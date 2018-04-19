using System;
using JetBrains.Annotations;

namespace Lykke.Service.BlockchainWallets.Contract.Models
{
    /// <summary>
    ///     Wallet
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
        ///     Blockchain address base.
        /// </summary>
        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        public string BaseAddress { get; set; }

        /// <summary>
        ///     Blockchain type (previously Blockchain Integration Layer Id).
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
        [UsedImplicitly(ImplicitUseKindFlags.Access), Obsolete("Use BlockchainType instead")]
        public string IntegrationLayerId { get; set; }

        /// <summary>
        ///     Blockchain Integration Layer Asset Id.
        /// </summary>
        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        public string IntegrationLayerAssetId { get; set; }
    }
}
