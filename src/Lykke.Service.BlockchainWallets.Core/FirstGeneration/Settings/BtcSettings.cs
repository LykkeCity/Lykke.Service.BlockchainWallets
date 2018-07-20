﻿namespace Lykke.Service.BlockchainWallets.Core.FirstGeneration
{
    public class BitcoinCoreSettings
    {
        public NetworkType NetworkType { get; set; }
        public string BitcoinCoreApiUrl { get; set; }
    }

    public enum NetworkType
    {
        Main,
        Testnet
    }
}
