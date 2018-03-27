using System.Threading.Tasks;
using Lykke.Service.BlockchainApi.Client;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IBlockchainIntegrationService
    {
        Task<bool> AssetIsSupportedAsync(string blockchain, string assetId);

        IBlockchainApiClient TryGetApiClient(string integrationLayerId);
    }
}
