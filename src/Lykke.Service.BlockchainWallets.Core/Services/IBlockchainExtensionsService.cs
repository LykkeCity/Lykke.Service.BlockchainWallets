﻿using Lykke.Service.BlockchainWallets.Core.DTOs;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IBlockchainExtensionsService : IInitializableService
    {
        bool IsCacheReadyForBlockchain(string blockchainType);
        bool? IsPublicAddressExtensionRequired(string blockchainType);
        bool? IsAddressMappingRequired(string blockchainType);
        AddressExtensionConstantsDto TryGetAddressExtensionConstants(string blockchainType);
        BlockchainAssetDto TryGetBlockchainAsset(string blockchainType, string assetId);
    }
}
