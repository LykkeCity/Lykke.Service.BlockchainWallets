using System;
using System.Collections.Generic;

namespace Lykke.Service.BlockchainWallets.Models
{
    /// <summary>
    ///     All clients wallets.
    /// </summary>
    public class ClientWalletsResponse
    {
        public IEnumerable<WalletResponse> Wallets { get; set; }

        public string ContinuationToken { get; set; }
    }
}
