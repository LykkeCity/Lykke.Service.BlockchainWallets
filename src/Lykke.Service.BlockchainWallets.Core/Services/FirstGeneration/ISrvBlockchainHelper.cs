using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;

namespace Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration
{
    public interface ISrvBlockchainHelper
    {
        Task<IWalletCredentials> GenerateWallets(string clientId, string clientPubKeyHex, string encodedPrivateKey, NetworkType networkType);
    }
}
