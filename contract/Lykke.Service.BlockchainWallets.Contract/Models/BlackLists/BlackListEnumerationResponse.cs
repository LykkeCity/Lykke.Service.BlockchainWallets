using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lykke.Service.BlockchainWallets.Contract.Models.BlackLists
{
    public class BlackListEnumerationResponse
    {
        public IEnumerable<BlackListResponse> List { get; set; }

        public string ContinuationToken { get; set; }
    }
}
