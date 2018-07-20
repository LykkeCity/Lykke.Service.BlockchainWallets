using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;

namespace Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration
{
    public interface ISrvEthereumHelper
    {
        Task<EthereumResponse<GetContractModel>> GetContractAsync(string assetId, string userAddress);

        Task<EthereumResponse<GetContractModel>> GetErc20DepositContractAsync(string userAddress);

        bool IsValidAddress(string address);

        bool IsValidAddressWithHexPrefix(string address);
    }
}
