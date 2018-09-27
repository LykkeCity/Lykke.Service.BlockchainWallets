using JetBrains.Annotations;

namespace Lykke.Service.BlockchainWallets.Contract.Models
{
    [PublicAPI]
    public class CreatedByResponse
    {
        public CreatorType CreatedBy { get; set; }
    }
}
