using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.Settings.BlockchainIntegrationSettings;

namespace Lykke.Service.BlockchainWallets.Core.Services
{
    public interface IBlockchainIntegrationService
    {
        bool BlockchainIsSupported(string blockchainType);

        IBlockchainApiClient TryGetApiClient(string blockchainType);

        IBlockchainApiClient GetApiClient(string blockchainType);

        ImmutableDictionary<string, BlockchainApiClient>.Enumerator GetApiClientsEnumerator();

        IEnumerable<KeyValuePair<string, BlockchainApiClient>> GetApiClientsEnumerable();

        BlockchainSettings GetSettings(string blockchainType);
    }
}
