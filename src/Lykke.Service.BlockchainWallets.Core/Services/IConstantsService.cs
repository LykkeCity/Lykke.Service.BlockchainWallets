using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.DTOs;


namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IConstantsService
    {
        Task<AddressExtensionConstantsDto> GetAddressExtensionConstantsAsync(string blockchainType);
    }
}
