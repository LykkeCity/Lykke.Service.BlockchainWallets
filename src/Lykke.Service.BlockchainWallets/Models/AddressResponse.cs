namespace Lykke.Service.BlockchainWallets.Models
{
    /// <summary>
    ///     Blockchain address.
    /// </summary>
    public class AddressResponse
    {
        /// <summary>
        ///     Blockchain address.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        ///     Blockchain address extension.
        /// </summary>
        public string AddressExtension { get; set; }
    }
}
