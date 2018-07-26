using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.BlockchainWallets.Core.FirstGeneration
{
    public interface IBcnCredentialsRecord
    {
        string Address { get; set; }
        string EncodedKey { get; set; }
        string PublicKey { get; set; }
        string AssetId { get; set; }
        string ClientId { get; set; }
        string AssetAddress { get; set; }
    }

    public class BcnCredentialsRecord : IBcnCredentialsRecord
    {
        public string Address { get; set; }
        public string EncodedKey { get; set; }
        public string PublicKey { get; set; }
        public string ClientId { get; set; }
        public string AssetAddress { get; set; }
        public string AssetId { get; set; }

        public static BcnCredentialsRecord Create(string assetId, 
            string clientId, 
            string address,
            string assetAddress,
            string pubKey, 
            string encodedKey = null)
        {
            return new BcnCredentialsRecord
            {
                Address = address,
                AssetAddress = assetAddress,
                AssetId = assetId,
                ClientId = clientId,
                EncodedKey = encodedKey,
                PublicKey = pubKey
            };
        }
    }
}
