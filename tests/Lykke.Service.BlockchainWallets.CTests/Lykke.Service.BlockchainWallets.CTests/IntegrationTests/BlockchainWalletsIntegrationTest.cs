using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.BlockchainWallets.Client.ClientGenerator;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.CTests.Common;
using Lykke.Service.BlockchainWallets.CTests.DelegatingHandlers;
using Lykke.Service.BlockchainWallets.CTests.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Xunit;

namespace Lykke.Service.BlockchainWallets.CTests.IntegrationTests
{
    public class BlockchainWalletsIntegrationTest :
        IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly CustomWebApplicationFactory<Startup> _factory;
        private readonly string _blockchainType = "EthereumClassic";

        public BlockchainWalletsIntegrationTest(CustomWebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task IntegrationTest_CreateWalletAsync_CheckBunchOfOperations()
        {
            var blockchainWalletClient = GenerateBlockchainWalletsClient();

            var guid = Guid.Parse("5b39a8a8-af3f-451d-8284-3c06980e2435");
            var etcWallet = await blockchainWalletClient.CreateWalletAsync(_blockchainType, guid, CreatorType.LykkeWallet);
            var createdWallet = await blockchainWalletClient.GetWalletAsync(_blockchainType, etcWallet.Address);
            await blockchainWalletClient.DeleteWalletAsync(_blockchainType, guid, etcWallet.Address);
            var existingWallet = await blockchainWalletClient.GetWalletAsync(_blockchainType, etcWallet.Address);
        }

        [Fact]
        public async Task IntegrationTest_CreateSeveralWallets_ReturnLast()
        {
            var blockchainWalletClient = GenerateBlockchainWalletsClient();

            var guid = Guid.Parse("5b39a8a8-af3f-451d-8284-3c06980e2435");
            var etcWallet1 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, guid, CreatorType.LykkeWallet);
            var etcWallet2 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, guid, CreatorType.LykkeWallet);
            var etcWallet3 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, guid, CreatorType.LykkeWallet);

            var createdWallets = await blockchainWalletClient.GetWalletsAsync(_blockchainType, guid, 10, null);
        }

        private BlockchainWalletsClient GenerateBlockchainWalletsClient()
        {
            var log = new Mock<ILog>();
            var logFactory = new Mock<ILogFactory>();
            logFactory.Setup(x => x.CreateLog(It.IsAny<object>())).Returns(log.Object);

            //var factory = new WebApplicationFactory<Startup>();
            var testClient = _factory.CreateClient();
            //interceptor redirects request to the TEST Server.
            var interceptor = new RequestInterceptorHandler(testClient);
            var blockchainWalletsApiFactory = new BlockchainWalletsApiFactory();
            var blockchainWalletClient =
                new Lykke.Service.BlockchainWallets.Client.BlockchainWalletsClient("http://localhost:5000",
                    logFactory.Object,
                    blockchainWalletsApiFactory,
                    1,
                    interceptor);

            return blockchainWalletClient;
        }
    }
}
