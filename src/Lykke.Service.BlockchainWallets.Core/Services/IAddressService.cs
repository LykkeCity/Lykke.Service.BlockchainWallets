using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IAddressService
    {
        Task<string> MergeAsync(string blockchainType, string baseAddress, string addressExtension);
        Task<string> GetUnderlyingAddressAsync(string blockchainType, string address);
        Task<string> GetVirtualAddressAsync(string blockchainType, string address);
    }
}
