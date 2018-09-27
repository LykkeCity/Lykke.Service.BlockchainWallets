using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lykke.Service.BlockchainWallets.CTests.Common
{
    public class IntegrationTestsWebApplicationFactory<TStartup>
        : WebApplicationFactory<Startup>
    {
    }
}
