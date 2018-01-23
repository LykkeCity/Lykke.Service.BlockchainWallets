using System.Threading.Tasks;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainSignService.Client;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IBlockchainIntegrationService
    {
        Task<bool> AssetIsSupportedAsync(string blockchain, string assetId);

        IBlockchainApiClient TryGetApiClient(string integrationLayerId);

        IBlockchainSignServiceClient TryGetSignServiceClient(string integrationLayerId);
    }
}
