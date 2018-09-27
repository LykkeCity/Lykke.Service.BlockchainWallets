using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.DTOs;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IWalletService
    {
        Task<WalletWithAddressExtensionDto> CreateWalletAsync(string blockchainType, string assetId, Guid clientId);

        Task<bool> DefaultWalletExistsAsync(string integrationLayerId, string assetId, Guid clientId);

        Task DeleteWalletsAsync(string blockchainType, string assetId, Guid clientId);

        Task<WalletWithAddressExtensionDto> TryGetDefaultAddressAsync(string blockchainType, string assetId, Guid clientId);
        
        Task<WalletWithAddressExtensionDto> TryGetFirstGenerationBlockchainAddressAsync(string assetId, Guid clientId);

        Task<Guid?> TryGetClientIdAsync(string blockchainType, string address);
        
        Task<bool> WalletExistsAsync(string blockchainType, string assetId, Guid clientId);

        Task<(IEnumerable<WalletWithAddressExtensionDto>, string continuationToken)> GetClientWalletsAsync(Guid clientId, int take, string continuationToken);

        Task<bool> DoesAssetExistAsync(string assetId);

        #region NewMethods

        Task<WalletWithAddressExtensionDto> CreateWalletAsync(string blockchainType, Guid clientId, CreatorType createdBy);

        Task<bool> WalletExistsAsync(string blockchainType, Guid clientId, string address);

        Task<WalletWithAddressExtensionDto> TryGetWalletAsync(string blockchainType, string address);

        Task DeleteWalletsAsync(string blockchainType, Guid clientId, string address);

        #endregion
    }
}
