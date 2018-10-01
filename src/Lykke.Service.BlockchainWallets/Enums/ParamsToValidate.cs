using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.BlockchainWallets.Enums
{
    [Flags]
    public enum ParamsToValidate
    {
        EmptyBlockchainType = 1,
        EmptyAddress = 2,
        EmptyAssetId = 4,
        EmptyClientId = 8,
        UnsupportedBlockchainType = 16 | EmptyBlockchainType,
        UnsupportedAssetId = 32 | EmptyAssetId | EmptyBlockchainType
    }
}
