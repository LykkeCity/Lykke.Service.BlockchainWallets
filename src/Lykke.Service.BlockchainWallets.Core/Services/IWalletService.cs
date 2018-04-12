using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.DTOs;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IWalletService
    {
        Task<WalletWithAddressExtensionDto> CreateWalletAsync(string blockchainType, string assetId, Guid clientId);

        Task<bool> DefaultWalletExistsAsync(string integrationLayerId, string assetId, Guid clientId);

        Task DeleteWalletsAsync(string integrationLayerId, string assetId, Guid clientId);

        Task<WalletWithAddressExtensionDto> TryGetDefaultAddressAsync(string integrationLayerId, string assetId, Guid clientId);

        Task<Guid?> TryGetClientIdAsync(string integrationLayerId, string assetId, string address);
        
        Task<bool> WalletExistsAsync(string integrationLayerId, string assetId, Guid clientId);

        Task<(IEnumerable<WalletWithAddressExtensionDto>, string continuationToken)> GetClientWalletsAsync(Guid clientId, int take, string continuationToken);
    }
}
