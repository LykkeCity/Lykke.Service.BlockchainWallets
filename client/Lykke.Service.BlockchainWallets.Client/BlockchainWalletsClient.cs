using System;
using Common.Log;

namespace Lykke.Service.BlockchainWallets.Client
{
    public class BlockchainWalletsClient : IBlockchainWalletsClient, IDisposable
    {
        private readonly ILog _log;

        public BlockchainWalletsClient(string serviceUrl, ILog log)
        {
            _log = log;
        }

        public void Dispose()
        {
            //if (_service == null)
            //    return;
            //_service.Dispose();
            //_service = null;
        }
    }
}
