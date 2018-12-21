using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.BlockchainWallets.Client.ClientGenerator;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.CTests.Common;
using Lykke.Service.BlockchainWallets.Tests.Common.DelegatingMessageHandlers;
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
        private readonly string _blockchainType = "Stellar";
        private readonly Guid _clientId = Guid.Parse("e18bf761-9d3b-4593-8c04-0677faf37bcb");
        private readonly string _assetId = "XLM";

        public BlockchainWalletsIntegrationTest(CustomWebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task IntegrationTest_GetClientWallets_ReturnInRightOrderAfterDeletion()
        {
            var blockchainWalletClient = GenerateBlockchainWalletsClient();

            var wallet1 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
            var wallet2 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
            var createdWallets = await blockchainWalletClient.TryGetClientWalletsAsync(_clientId, 200, null);
            await blockchainWalletClient.DeleteWalletAsync(_blockchainType, _clientId, wallet2.Address);
            var existingDepositsAfterDeletion = await blockchainWalletClient.TryGetClientWalletsAsync(_clientId, 200, null);
            var arrayFromDB = createdWallets.Wallets.ToArray();
            var arrayAfterDeletion = existingDepositsAfterDeletion.Wallets.ToArray();

            var etcDeposits = arrayFromDB.Where(x => x.BlockchainType == _blockchainType);
            var etcDepositsCount = etcDeposits.Count();
            var latest = etcDeposits.FirstOrDefault();

            var etcDeposits2 = arrayAfterDeletion.Where(x => x.BlockchainType == _blockchainType);
            var etcDepositsCount2 = etcDeposits2.Count();
            var latest2 = etcDeposits2.FirstOrDefault();

            Assert.True(wallet2.Address == latest.Address);
            Assert.True(etcDepositsCount == 1);

            Assert.True(wallet1.Address == latest2.Address);
            Assert.True(etcDepositsCount2 == 1);
        }

        [Fact]
        public async Task IntegrationTest_GetClientWallets_ReturnInRightOrder()
        {
            var blockchainWalletClient = GenerateBlockchainWalletsClient();

            var wallet1 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
            var wallet2 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
            var createdWallets = await blockchainWalletClient.TryGetClientWalletsAsync(_clientId, 200, null);
            var arrayFromDB = createdWallets.Wallets.ToArray();

            var etcDeposits = arrayFromDB.Where(x => x.BlockchainType == _blockchainType);
            var etcDepositsCount = etcDeposits.Count();
            var latest = arrayFromDB.Where(x => x.BlockchainType == _blockchainType).FirstOrDefault();

            Assert.True(wallet2.Address == latest.Address);
            Assert.True(etcDepositsCount == 1);
        }

        [Fact]
        public async Task IntegrationTest_CheckFlow_CheckBunchOfOperations()
        {
            var blockchainWalletClient = GenerateBlockchainWalletsClient();

            var wallet = await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
            var createdWallet = await blockchainWalletClient.TryGetWalletAsync(_blockchainType, wallet.Address);
            await blockchainWalletClient.DeleteWalletAsync(_blockchainType, _clientId, wallet.Address);
            var existingWallet = await blockchainWalletClient.TryGetWalletAsync(_blockchainType, wallet.Address);

            Assert.Equal(wallet.Address, createdWallet.Address);
            Assert.True(existingWallet == null);
        }

        [Fact]
        public async Task IntegrationTest_CreateSeveralWallets_ReturnInRightOrder()
        {
            var blockchainWalletClient = GenerateBlockchainWalletsClient();

            var wallet1 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
            var wallet2 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
            var wallet3 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
            var wallet4 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
            var wallet5 = await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
            var list = new List<Lykke.Service.BlockchainWallets.Contract.Models.BlockchainWalletResponse>()
            {
                wallet5,
                wallet4,
                wallet3,
                wallet2,
                wallet1
            };

            var createdWallets = await blockchainWalletClient.TryGetWalletsAsync(_blockchainType, _clientId, 5, null);
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
            
            string cToken = null;

            await RemoveAllWalletsForClient(blockchainWalletClient, _clientId);

            var leftovers =
                await blockchainWalletClient.TryGetWalletsAsync(_blockchainType, _clientId, 100, cToken);

            Assert.True((leftovers?.Wallets?.Count() ?? 0) == 0);
        }

        private async Task RemoveAllWalletsForClient(BlockchainWalletsClient blockchainWalletClient, Guid clientId)
        {
            string cToken = null;
            do
            {
                var createdWallets =
                    await blockchainWalletClient.TryGetWalletsAsync(_blockchainType, clientId, 100, cToken);

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
            
            await RemoveAllWalletsForClient(blockchainWalletClient, _clientId);
            var createdWallet2 = await blockchainWalletClient.TryGetAddressAsync(_blockchainType, _assetId, _clientId);
            var wallet = await blockchainWalletClient.CreateWalletAsync(_blockchainType, _assetId, _clientId);
            var createdWallet = await blockchainWalletClient.GetAddressAsync(_blockchainType, _assetId, _clientId);
            var allWallets = await blockchainWalletClient.GetAllWalletsAsync(_clientId);
            await blockchainWalletClient.DeleteWalletAsync(_blockchainType, _assetId, _clientId);

            Assert.Equal(wallet.Address, createdWallet.Address);
            Assert.True(allWallets?.Any(x => x.Address == wallet.Address));
            await Assert.ThrowsAsync<Lykke.Service.BlockchainWallets.Client.ResultValidationException>(async () =>
            {
                var retrivedTwiceWallet = await blockchainWalletClient.GetAddressAsync(_blockchainType, _assetId, _clientId);
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
