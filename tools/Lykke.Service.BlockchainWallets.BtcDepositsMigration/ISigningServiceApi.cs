using System.Threading.Tasks;
using RestEase;

namespace Lykke.Service.BlockchainWallets.BtcDepositsMigration
{
    public interface ISigningServiceApi
    {
        [Header("apiKey")] string ApiKey { get; set; }

        [Get("/api/bitcoin/getkey")]
        Task<KeyResponse> GetPrivateKey([Query] string address);
    }

    public class KeyResponse
    {
        public string PrivateKey { get; set; }
    }
}
