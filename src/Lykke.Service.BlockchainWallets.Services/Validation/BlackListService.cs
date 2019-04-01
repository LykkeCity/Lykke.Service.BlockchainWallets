using System;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainWallets.Core.DTOs.Validation;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Core.Services.Validation;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.Exceptions;
using Lykke.Service.BlockchainWallets.Core.Services;

namespace Lykke.Service.BlockchainWallets.Services.Validation
{
    public class BlackListService : IBlackListService
    {
        private readonly IBlackListRepository _blackListRepository;
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;

        public BlackListService(IBlackListRepository blackListRepository,
            IBlockchainIntegrationService blockchainIntegrationService)
        {
            _blackListRepository = blackListRepository;
            _blockchainIntegrationService = blockchainIntegrationService;
        }

        public async Task<bool> IsBlockedWithoutAddressValidationAsync(string blockchainType, string blockedAddress)
        {
            await ThrowOnNotSupportedBlockchainType(blockchainType);

            var blackList = await _blackListRepository.TryGetAsync(blockchainType, blockedAddress);

            if (blackList == null)
            {
                return false;
            }

            var isBlocked = IsBlocked(blockedAddress, blackList);

            return isBlocked;
        }

        public async Task<bool> IsBlockedAsync(string blockchainType, string blockedAddress)
        {
            await ThrowOnNotSupportedBlockchainType(blockchainType, blockedAddress);

            var blackList = await _blackListRepository.TryGetAsync(blockchainType, blockedAddress);

            if (blackList == null)
            {
                return false;
            }

            var isBlocked = IsBlocked(blockedAddress, blackList);

            return isBlocked;
        }

        public async Task<BlackListModel> TryGetAsync(string blockchainType, string blockedAddress)
        {
            await ThrowOnNotSupportedBlockchainType(blockchainType, blockedAddress);

            var model = await _blackListRepository.TryGetAsync(blockchainType, blockedAddress);

            return model;
        }

        public async Task<(IEnumerable<BlackListModel>, string continuationToken)> TryGetAllAsync(string blockchainType, int take, string continuationToken = null)
        {
            await ThrowOnNotSupportedBlockchainType(blockchainType);

            var (models, newToken) = await _blackListRepository.TryGetAllAsync(blockchainType, take, continuationToken);

            return (models, newToken);
        }

        public async Task SaveAsync(BlackListModel model)
        {
            await ThrowOnNotSupportedBlockchainType(model?.BlockchainType ?? "", model?.BlockedAddress);

            await _blackListRepository.SaveAsync(model);
        }

        public async Task DeleteAsync(string blockchainType, string blockedAddress)
        {
            await ThrowOnNotSupportedBlockchainType(blockchainType, blockedAddress);

            await _blackListRepository.DeleteAsync(blockchainType, blockedAddress);
        }

        /// <exception cref="OperationException"></exception>
        private Task<IBlockchainApiClient> ThrowOnNotSupportedBlockchainType(string blockchainType)
        {
            IBlockchainApiClient blockchainClient;

            try
            {
                blockchainClient = _blockchainIntegrationService.GetApiClient(blockchainType); //throws
            }
            catch (ArgumentException)
            {
                throw new OperationException($"{blockchainType} is not a valid type", OperationErrorCode.None);
            }


            return Task.FromResult(blockchainClient);
        }

        private async Task ThrowOnNotSupportedBlockchainType(string blockchainType, string blockedAddress)
        {
            var blockchainClient = await ThrowOnNotSupportedBlockchainType(blockchainType);//throws
            if (string.IsNullOrEmpty(blockedAddress) ||
                !await blockchainClient.IsAddressValidAsync(blockedAddress))
            {
                throw new OperationException($"{blockedAddress} is not a valid address for {blockchainType}", OperationErrorCode.None);
            }
        }

        private static bool IsBlocked(string blockedAddress, BlackListModel blackList)
        {
            var isBlocked = blackList.IsCaseSensitive && blockedAddress == blackList.BlockedAddress ||
                            !blackList.IsCaseSensitive && blockedAddress.ToLower() == blackList.BlockedAddressLowCase;
            return isBlocked;
        }
    }
}
