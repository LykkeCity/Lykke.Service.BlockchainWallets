using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IAddressService
    {
        Task<string> MergeAsync(string blockchainType, string publicAddress, string addressExtension);
    }
}
