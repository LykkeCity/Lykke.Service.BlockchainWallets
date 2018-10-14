using System.Collections.Generic;

namespace Lykke.Service.BlockchainWallets.Contract.Models
{
    /// <summary>
    ///     All clients wallets.
    /// </summary>
    public class BlockchainWalletsResponse
    {
        public IEnumerable<BlockchainWalletResponse> Wallets { get; set; }

        public string ContinuationToken { get; set; }
    }
}
