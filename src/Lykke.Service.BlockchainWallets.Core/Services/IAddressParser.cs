using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.DTOs;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IAddressParser
    {
        Task<AddressParseResultDto> ExtractAddressParts(string blockchainType, string address);
    }
}
