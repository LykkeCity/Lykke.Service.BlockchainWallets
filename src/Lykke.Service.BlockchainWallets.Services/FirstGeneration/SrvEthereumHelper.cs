using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;
using Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration;
using Lykke.Service.EthereumCore.Client;
using Lykke.Service.EthereumCore.Client.Models;
using Nethereum.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.Services.FirstGeneration
{
    public class SrvEthereumHelper : ISrvEthereumHelper
    {
        private readonly static Regex _ethAddressIgnoreCaseRegex = new Regex("^(0x)?[0-9a-f]{40}$",
            RegexOptions.Compiled
            | RegexOptions.IgnoreCase);

        private readonly static Regex _ethAddressRegex = new Regex("(0x)?[0-9a-f]{40}$", RegexOptions.Compiled);
        private readonly static Regex _ethAddressCapitalRegex = new Regex("^(0x)?[0-9A-F]{40}$", RegexOptions.Compiled);

        private readonly static Regex _ethAddressWithHexPrefixIgnoreCaseRegex = new Regex("^0x[0-9a-f]{40}$",
            RegexOptions.Compiled
            | RegexOptions.IgnoreCase);

        private readonly static Regex _ethAddressWithHexPrefixRegex =
            new Regex("^0x[0-9a-f]{40}$", RegexOptions.Compiled);

        private readonly static Regex _ethAddressWithHexPrefixCapitalRegex =
            new Regex("^0x[0-9A-F]{40}$", RegexOptions.Compiled);

        private readonly IEthereumCoreAPI _ethereumApi;
        private readonly AddressUtil _addressUtil;
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;

        public SrvEthereumHelper(
            IEthereumCoreAPI ethereumApi,
            IAssetsServiceWithCache assetsServiceWithCache)
        {
            _addressUtil = new AddressUtil();
            _ethereumApi = ethereumApi;
            _assetsServiceWithCache = assetsServiceWithCache;
        }

        public bool IsValidAddress(string address)
        {
            if (!_ethAddressIgnoreCaseRegex.IsMatch(address))
            {
                // check if it has the basic requirements of an address
                return false;
            }
            else if (_ethAddressRegex.IsMatch(address) ||
                     _ethAddressCapitalRegex.IsMatch(address))
            {
                // If it's all small caps or all all caps, return true
                return true;
            }
            else
            {
                // Check each case
                return _addressUtil.IsChecksumAddress(address);
            }

            ;
        }

        public bool IsValidAddressWithHexPrefix(string address)
        {
            if (!_ethAddressWithHexPrefixIgnoreCaseRegex.IsMatch(address))
            {
                // check if it has the basic requirements of an address
                return false;
            }
            else if (_ethAddressWithHexPrefixRegex.IsMatch(address) ||
                     _ethAddressWithHexPrefixCapitalRegex.IsMatch(address))
            {
                // If it's all small caps or all all caps, return true
                return true;
            }
            else
            {
                // Check each case
                return _addressUtil.IsChecksumAddress(address);
            }

            ;
        }

        public async Task<EthereumResponse<GetContractModel>> GetContractAsync(string assetId, string userAddress)
        {
            Asset asset = await _assetsServiceWithCache.TryGetAssetAsync(assetId);

            var response = await _ethereumApi.ApiTransitionCreatePostAsync(new CreateTransitionContractModel
            {
                CoinAdapterAddress = asset.AssetAddress,
                UserAddress = userAddress
            });

            if (response is ApiException error)
            {
                return new EthereumResponse<GetContractModel>
                {
                    Error = new Core.FirstGeneration.ErrorResponse
                    {
                        Code = error.Error.Code.ToString(),
                        Message = error.Error.Message
                    }
                };
            }

            var res = response as RegisterResponse;
            if (res != null)
            {
                return new EthereumResponse<GetContractModel>
                {
                    Result = new GetContractModel
                    {
                        Contract = res.Contract
                    }
                };
            }

            throw new Exception("Unknown response");
        }

        public async Task<EthereumResponse<GetContractModel>> GetErc20DepositContractAsync(string userAddress)
        {
            var response = await _ethereumApi.ApiErc20depositsPostAsync(userAddress);

            if (response is ApiException error)
            {
                return new EthereumResponse<GetContractModel>
                {
                    Error = new Core.FirstGeneration.ErrorResponse
                    {
                        Code = error.Error.Code.ToString(),
                        Message = error.Error.Message
                    }
                };
            }

            var res = response as RegisterResponse;
            if (res != null)
            {
                return new EthereumResponse<GetContractModel>
                {
                    Result = new GetContractModel
                    {
                        Contract = res.Contract
                    }
                };
            }

            throw new Exception("Unknown response");
        }
    }
}
