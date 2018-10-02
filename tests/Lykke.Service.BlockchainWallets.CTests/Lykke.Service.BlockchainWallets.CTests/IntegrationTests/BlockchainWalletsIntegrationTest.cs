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
        public async Task IntegrationTest_GetClientWallets_ReturnInRightOrderAfterDeletion()
        {
            var blockchainWalletClient = GenerateBlockchainWalletsClient();

            var clientId = Guid.Parse("5b39a8a8-af3f-451d-8284-3c06980e2435");
            var etcWallet1 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, clientId, CreatorType.LykkeWallet);
            var etcWallet2 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, clientId, CreatorType.LykkeWallet);
            var createdWallets = await blockchainWalletClient.GetClientWalletsAsync(clientId, 200, null);
            await blockchainWalletClient.DeleteWalletAsync(_blockchainType, clientId, etcWallet2.Address);
            var existingDepositsAfterDeletion = await blockchainWalletClient.GetClientWalletsAsync(clientId, 200, null);
            var arrayFromDB = createdWallets.Wallets.ToArray();
            var arrayAfterDeletion = existingDepositsAfterDeletion.Wallets.ToArray();

            var etcDeposits = arrayFromDB.Where(x => x.BlockchainType == _blockchainType);
            var etcDepositsCount = etcDeposits.Count();
            var latest = etcDeposits.FirstOrDefault();

            var etcDeposits2 = arrayAfterDeletion.Where(x => x.BlockchainType == _blockchainType);
            var etcDepositsCount2 = etcDeposits2.Count();
            var latest2 = etcDeposits2.FirstOrDefault();

            Assert.True(etcWallet2.Address == latest.Address);
            Assert.True(etcDepositsCount == 1);

            Assert.True(etcWallet1.Address == latest2.Address);
            Assert.True(etcDepositsCount2 == 1);
        }

        [Fact]
        public async Task IntegrationTest_GetClientWallets_ReturnInRightOrder()
        {
            var blockchainWalletClient = GenerateBlockchainWalletsClient();

            var clientId = Guid.Parse("5b39a8a8-af3f-451d-8284-3c06980e2435");
            var etcWallet1 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, clientId, CreatorType.LykkeWallet);
            var etcWallet2 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, clientId, CreatorType.LykkeWallet);
            var createdWallets = await blockchainWalletClient.GetClientWalletsAsync(clientId, 200, null);
            var arrayFromDB = createdWallets.Wallets.ToArray();

            var etcDeposits = arrayFromDB.Where(x => x.BlockchainType == _blockchainType);
            var etcDepositsCount = etcDeposits.Count();
            var latest = arrayFromDB.Where(x => x.BlockchainType == _blockchainType).FirstOrDefault();

            Assert.True(etcWallet2.Address == latest.Address);
            Assert.True(etcDepositsCount == 1);
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

            await RemoveAllWalletsForClient(blockchainWalletClient, clientId);

            var leftovers =
                await blockchainWalletClient.GetWalletsAsync(_blockchainType, clientId, 100, cToken);

            Assert.True((leftovers?.Wallets?.Count() ?? 0) == 0);
        }

        private async Task RemoveAllWalletsForClient(BlockchainWalletsClient blockchainWalletClient, Guid clientId)
        {
            string cToken = null;
            do
            {
                var createdWallets =
                    await blockchainWalletClient.GetWalletsAsync(_blockchainType, clientId, 100, cToken);

                if (createdWallets == null)
                {
                    cToken = null;
                    continue;
                }

                cToken = createdWallets.ContinuationToken;

                foreach (var wallet in createdWallets.Wallets)
                {
                    try
                    {
                        await blockchainWalletClient.DeleteWalletAsync(_blockchainType, wallet.ClientId,
                            wallet.Address);
                    }
                    catch
                    {
                    }
                }
            } while (!string.IsNullOrEmpty(cToken));
        }

        [Fact]
        public async Task IntegrationTest_CheckDeprecatedMethods_WorksCorrect()
        {
            var blockchainWalletClient = GenerateBlockchainWalletsClient();
            var assetId = "ETC";
            var clientId = Guid.Parse("5b39a8a8-af3f-451d-8284-3c06980e2435");
            await RemoveAllWalletsForClient(blockchainWalletClient, clientId);
            var createdWallet2 = await blockchainWalletClient.TryGetAddressAsync(_blockchainType, assetId, clientId);
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
