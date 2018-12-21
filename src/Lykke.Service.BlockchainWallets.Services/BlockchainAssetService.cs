using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.Services;

namespace Lykke.Service.BlockchainWallets.Services
{
    public class BlockchainAssetService : IBlockchainAssetService
    {
        private readonly IBlockchainExtensionsService _blockchainExtensionsService;

        public BlockchainAssetService(IBlockchainExtensionsService blockchainExtensionsService)
        {
            _blockchainExtensionsService = blockchainExtensionsService;
        }

        public bool IsAssetSupported(string blockchainType, string assetId)
        {
            if (blockchainType == SpecialBlockchainTypes.FirstGenerationBlockchain)
                return true;

            return _blockchainExtensionsService.TryGetBlockchainAsset(blockchainType, assetId) != null;
        }
    }
}
