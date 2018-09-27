using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Lykke.Service.BlockchainWallets.CTests
{
    public class BasicTests //: IClassFixture<WebApplicationFactory<Startup>>
    {
        //private readonly WebApplicationFactory<Startup> _factory;

        public BasicTests()
        {
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/Index")]
        [InlineData("/About")]
        [InlineData("/Privacy")]
        [InlineData("/Contact")]
        public async Task Get_EndpointsReturnSuccessAndCorrectContentType(string url)
        {

        }
    }
}
