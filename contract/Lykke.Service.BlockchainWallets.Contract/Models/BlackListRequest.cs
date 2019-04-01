using Microsoft.AspNetCore.Mvc;
namespace Lykke.Service.BlockchainWallets.Contract.Models
{
    public class BlackListRequest
    {
        [FromRoute(Name = "blockchainType")]
        public string BlockchainType { get; set; }

        [FromRoute(Name = "address")]
        public string Address { get; set; }

        [FromQuery(Name = "isCaseSensitive")]
        public bool IsCaseSensitive { get; set; }
    }
}
