using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IBlockchainAssetService
    {
        bool IsAssetSupported(string blockchainType, string assetId);
    }
}
