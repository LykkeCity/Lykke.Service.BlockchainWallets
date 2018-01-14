using System.Threading.Tasks;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainSignService.Client;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IBlockchainIntegrationService
    {
        Task<bool> AssetIsSupported(string blockchain, string assetId);

        IBlockchainApiClient GetApiClient(string integrationLayerId);

        IBlockchainSignServiceClient GetSignServiceClient(string integrationLayerId);
    }
}
