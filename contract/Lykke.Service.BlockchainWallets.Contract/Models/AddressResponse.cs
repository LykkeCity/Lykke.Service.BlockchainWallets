using JetBrains.Annotations;

namespace Lykke.Service.BlockchainWallets.Contract.Models
{
    /// <summary>
    ///     Blockchain address.
    /// </summary>
    [PublicAPI]
    public class AddressResponse
    {
        /// <summary>
        ///     Blockchain address.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        ///     Blockchain address base.
        /// </summary>
        public string BaseAddress { get; set; }

        /// <summary>
        ///     Blockchain address extension.
        /// </summary>
        public string AddressExtension { get; set; }
    }
}
