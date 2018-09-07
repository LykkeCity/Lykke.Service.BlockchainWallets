using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.BlockchainWallets.Client
{
    public class DuplicationWalletException:Exception
    {
        public DuplicationWalletException(string message) : base(message)
        {

        }
    }
}
