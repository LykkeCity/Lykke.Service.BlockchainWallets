using System.Threading.Tasks;
using Lykke.Service.BlockchainApi.Client;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IBlockchainIntegrationService
    {
        Task<bool> AssetIsSupportedAsync(string blockchainType, string assetId);

        bool BlockchainIsSupported(string blockchainType);

        IBlockchainApiClient TryGetApiClient(string blockchainType);
    }
}
