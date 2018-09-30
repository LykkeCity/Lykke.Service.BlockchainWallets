using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.BlockchainWallets.Client.ClientGenerator;
using Lykke.Service.BlockchainWallets.Client.DelegatingMessageHandlers;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.CTests.Common;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task IntegrationTest_CheckFlow_CheckBunchOfOperations()
        {
            var blockchainWalletClient = GenerateBlockchainWalletsClient();

            var clientId = Guid.Parse("5b39a8a8-af3f-451d-8284-3c06980e2435");
            var etcWallet = await blockchainWalletClient.CreateWalletAsync(_blockchainType, clientId, CreatorType.LykkeWallet);
            var createdWallet = await blockchainWalletClient.GetWalletAsync(_blockchainType, etcWallet.Address);
            await blockchainWalletClient.DeleteWalletAsync(_blockchainType, clientId, etcWallet.Address);
            var existingWallet = await blockchainWalletClient.GetWalletAsync(_blockchainType, etcWallet.Address);

            Assert.Equal(etcWallet.Address, createdWallet.Address);
            Assert.True(existingWallet == null);
        }

        [Fact]
        public async Task IntegrationTest_CreateSeveralWallets_ReturnInRightOrder()
        {
            var blockchainWalletClient = GenerateBlockchainWalletsClient();

            var clientId = Guid.Parse("5b39a8a8-af3f-451d-8284-3c06980e2435");
            var etcWallet1 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, clientId, CreatorType.LykkeWallet);
            var etcWallet2 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, clientId, CreatorType.LykkeWallet);
            var etcWallet3 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, clientId, CreatorType.LykkeWallet);
            var etcWallet4 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, clientId, CreatorType.LykkeWallet);
            var etcWallet5 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, clientId, CreatorType.LykkeWallet);
            var list = new List<Lykke.Service.BlockchainWallets.Contract.Models.BlockchainWalletResponse>()
            {
                etcWallet5,
                etcWallet4,
                etcWallet3,
                etcWallet2,
                etcWallet1
            };

            var createdWallets = await blockchainWalletClient.GetWalletsAsync(_blockchainType, clientId, 5, null);
            var arrayFromDB = createdWallets.Wallets.ToArray();

            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(list[i].Address, arrayFromDB[i].Address);
            }
        }

        [Fact]
        public async Task IntegrationTest_DeleteAllWallets_NoOneIsLeft()
        {
            var blockchainWalletClient = GenerateBlockchainWalletsClient();
            var clientId = Guid.Parse("5b39a8a8-af3f-451d-8284-3c06980e2435");
            string cToken = null;

            do
            {
                try
                {
                    var createdWallets =
                        await blockchainWalletClient.GetWalletsAsync(_blockchainType, clientId, 100, cToken);

                    if (createdWallets == null)
                        continue;

                    cToken = createdWallets.ContinuationToken;

                    foreach (var wallet in createdWallets.Wallets)
                    {
                        await blockchainWalletClient.DeleteWalletAsync(_blockchainType, wallet.ClientId,
                            wallet.Address);
                    }
                }
                finally
                {
                }
            } while (!string.IsNullOrEmpty(cToken));

            var leftovers =
                await blockchainWalletClient.GetWalletsAsync(_blockchainType, clientId, 100, cToken);

            Assert.True((leftovers?.Wallets?.Count() ?? 0) == 0);
        }

        [Fact]
        public async Task IntegrationTest_CheckDeprecatedMethods_WorksCorrect()
        {
            var blockchainWalletClient = GenerateBlockchainWalletsClient();
            var assetId = "ETC";
            var clientId = Guid.Parse("5b39a8a8-af3f-451d-8284-3c06980e2435");
            var etcWallet = await blockchainWalletClient.CreateWalletAsync(_blockchainType, assetId, clientId);
            var createdWallet = await blockchainWalletClient.GetAddressAsync(_blockchainType, assetId, clientId);
            var allWallets = await blockchainWalletClient.GetAllWalletsAsync(clientId);
            await blockchainWalletClient.DeleteWalletAsync(_blockchainType, assetId, clientId);

            Assert.Equal(etcWallet.Address, createdWallet.Address);
            Assert.True(allWallets?.Any(x => x.Address == etcWallet.Address));
            await Assert.ThrowsAsync<Lykke.Service.BlockchainWallets.Client.ResultValidationException>(async () =>
            {
                var retrivedTwiceWallet = await blockchainWalletClient.GetAddressAsync(_blockchainType, assetId, clientId);
            });
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
