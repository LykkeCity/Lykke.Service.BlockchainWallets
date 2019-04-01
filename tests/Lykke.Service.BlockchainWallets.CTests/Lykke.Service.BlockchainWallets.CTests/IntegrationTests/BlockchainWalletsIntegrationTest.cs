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
using Lykke.Service.BlockchainWallets.Contract.Models;
using Xunit;

namespace Lykke.Service.BlockchainWallets.CTests.IntegrationTests
{
    public class BlockchainWalletsIntegrationTest :
        IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private const string _etcAssetId = "915c4074-ec20-40ed-b8b7-5e3cc2f303b1";
        private const string _stellarAssetId = "b5a0389c-fe57-425f-ab17-af41638f6b89";
        private const string _stellarBlockchainType = "Stellar";
        private const string _etcBlockchainType = "EthereumClassic";
        private readonly Guid _clientIdForValidityChecks = Guid.Parse("cbecad9b-9fcb-4f3c-9c09-accbee4059db");
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

            var wallet1 =
                await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
            var wallet2 =
                await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
            var createdWallets = await blockchainWalletClient.TryGetClientWalletsAsync(_clientId, 200, null);
            await blockchainWalletClient.DeleteWalletAsync(_blockchainType, _clientId, wallet2.Address);
            var existingDepositsAfterDeletion =
                await blockchainWalletClient.TryGetClientWalletsAsync(_clientId, 200, null);
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

            var wallet1 =
                await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
            var wallet2 =
                await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
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

            var wallet =
                await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
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

            var wallet1 =
                await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
            var wallet2 =
                await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
            var wallet3 =
                await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
            var wallet4 =
                await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
            var wallet5 =
                await blockchainWalletClient.CreateWalletAsync(_blockchainType, _clientId, CreatorType.LykkeWallet);
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
                var retrivedTwiceWallet =
                    await blockchainWalletClient.GetAddressAsync(_blockchainType, _assetId, _clientId);
            });
        }

        #region ValidityCheck

        [Theory]
        //[InlineData("ETH", "0x81b7E08F65Bdf5648606c89998A9CC8164397647", true)]
        [InlineData("ETH", "0x406561f72e25ab41200fa3d52badc5a21", false)]
        //Cashout on hotwallet is forbidden
        [InlineData("b5a0389c-fe57-425f-ab17-af41638f6b89", "GDF4MNKB57VPSF2ZAM36YEXH6TFEXQGQT4IJVR3IOMZQIFC2B44Z4HBL", false)]
        //Cashout on nonexistent deposit is forbidden
        [InlineData("b5a0389c-fe57-425f-ab17-af41638f6b89", "GDF4MNKB57VPSF2ZAM36YEXH6TFEXQGQT4IJVR3IOMZQIFC2B44Z4HBL$gmp91dzbofqrmxdw4hqt4idwyw", false)]
        [InlineData("b5a0389c-fe57-425f-ab17-af41638f6b89", "GDF4MNKB57VPSF2ZAM36YEXH6TFEXQGQT4IJVR3IOMZQIFC2B44Z4HBL$dpkyqq1ya1kw7cscxkk1545asw", false)]
        [InlineData("b5a0389c-fe57-425f-ab17-af41638f6b89", "GDF4MNKB57VPSF2ZAM36YEXH6TFEXQGQT4IJVR3IOMZQIFC2B44Z4HBL$s7944tp9w7zr9pccmq7epgdume", true)]
        //[InlineData("d1a7ffea-2ca1-48b6-a41f-a7058ddb0dfa", "lykkedev$0sdfsdf$", false)]
        //[InlineData("d1a7ffea-2ca1-48b6-a41f-a7058ddb0dfa", "lykkedev$$$WHY$$$", false)]
        //[InlineData("20ce0468-917e-4097-abba-edf7c8600cfb", "lykkedev2018:123::::", false)]
        public async Task ValidateCashoutAsync_ExecuteOnDataSet(
            string assetId, string destinationAddress, bool isValidExpected)
        {
            var blockchainCashoutPreconditionsCheckClient = GenerateBlockchainWalletsClient();

            var result = await
                blockchainCashoutPreconditionsCheckClient.CashoutCheckAsync( 
                    destinationAddress,
                    assetId,
                    _clientIdForValidityChecks,
                    1);

            Assert.Equal(isValidExpected, result.IsAllowed);
        }

        #endregion ValidityCheck

        #region BlackLists

        [Fact]
        public async Task CheckBlackListPositiveRestFlow_AllOperationsWorkAsExpected()
        {
            var blockchainCashoutPreconditionsCheckClient = GenerateBlockchainWalletsClient();
            string blockedAddress = "GA4FSFQNHZ5A7VVRDC3UKLUVHX7BAZGRZ432XOAFVQJMYULPKKPPE7PY";
            var newBlackList = new BlackListRequest()
            {
                BlockchainType = _stellarBlockchainType,
                Address = blockedAddress,
                IsCaseSensitive = false
            };

            await blockchainCashoutPreconditionsCheckClient.CreateBlackListAsync(newBlackList);
            var blackList =
                await blockchainCashoutPreconditionsCheckClient.GetBlackListAsync(_stellarBlockchainType,
                    blockedAddress);
            var allEtcBlackLists =
                await blockchainCashoutPreconditionsCheckClient.GetBlackListsAsync(_stellarBlockchainType, 500, null);
            await blockchainCashoutPreconditionsCheckClient.DeleteBlackListAsync(_stellarBlockchainType,
                blockedAddress);
            var blackListAfterDeletion =
                await blockchainCashoutPreconditionsCheckClient.GetBlackListAsync(_stellarBlockchainType,
                    blockedAddress);
            var allEtcBlackListsAfterDeletion =
                await blockchainCashoutPreconditionsCheckClient.GetBlackListsAsync(_stellarBlockchainType, 500, null);

            Assert.True(blackList != null);
            Assert.True(allEtcBlackLists.List.FirstOrDefault(x => x.BlockedAddress == blockedAddress) != null);
            Assert.True(blackListAfterDeletion == null);
            Assert.True(allEtcBlackListsAfterDeletion.List.FirstOrDefault(x => x.BlockedAddress == blockedAddress) ==
                        null);
        }

        [Fact]
        public async Task AddToBlackListAsync_NotValidParameters_ExcetionThrow()
        {
            var blockchainCashoutPreconditionsCheckClient = GenerateBlockchainWalletsClient();
            string blockedAddress = "";
            var newBlackList = new BlackListRequest()
            {
                BlockchainType = _etcBlockchainType,
                Address = blockedAddress,
                IsCaseSensitive = false
            };

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await blockchainCashoutPreconditionsCheckClient.CreateBlackListAsync(newBlackList);
            });
        }

        #endregion

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
