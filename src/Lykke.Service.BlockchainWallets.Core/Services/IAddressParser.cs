using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.DTOs;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IAddressParser
    {
        Task<AddressParseResultDto> ExtractAddressParts(string blockchainType, string address);
    }
}
