using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface ICapabilitiesService
    {
        Task<bool> IsPublicAddressExtensionRequiredAsync(string blockchainType);
    }
}
