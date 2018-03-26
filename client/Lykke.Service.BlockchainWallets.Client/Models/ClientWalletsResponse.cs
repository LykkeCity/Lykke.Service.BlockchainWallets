using System;
using System.Collections.Generic;
using Lykke.Service.BlockchainWallets.Client.Models;

namespace Lykke.Service.BlockchainWallets.Client.Models
{
    /// <summary>
    ///     All clients wallets.
    /// </summary>
    public class ClientWalletsResponse
    {
        public IEnumerable<ClientWalletResponse> Wallets { get; set; }

        public string ContinuationToken { get; set; }
    }
}
