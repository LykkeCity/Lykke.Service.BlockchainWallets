using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainWallets.Core.DTOs.Validation;
using Lykke.Service.BlockchainWallets.Services.Validation;
using Lykke.Service.BlockchainWallets.Tests.Validation;
using Moq;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.BlockchainWallets.Core.Services;
using Xunit;

namespace Lykke.Service.BCPCheck.Tests
{
    //Naming: MethodName__TestCase__ExpectedResult
    public class BlackListServiceTest
    {
        private const string BlockchainType = "EthereumClassic";
        private const string BlockedAddressValid = "0xeb574cD5A407Fefa5610fCde6Aec13D983BA527c";
        private const string BlockedAddressNotValid = "12323545tsrdfe54rydfgryg65uhjdfesd";
        private BlackListService _logic;

        public BlackListServiceTest()
        {
            var repo = new BlackListRepositoryFake();
            var blockchainApiClientProviderMock = new Mock<IBlockchainIntegrationService>();
            var blockchainApiClient = new Mock<IBlockchainApiClient>();
            blockchainApiClient
                .Setup(x => x.IsAddressValidAsync(BlockedAddressValid))
                .Returns(Task.FromResult(true));
            blockchainApiClient
                .Setup(x => x.IsAddressValidAsync(BlockedAddressValid.ToLower()))
                .Returns(Task.FromResult(true));

            for (int i = 0; i < 9; i++)
            {
                var z = i;
                blockchainApiClient
                    .Setup(x => x.IsAddressValidAsync($"0x{z}..."))
                    .Returns(Task.FromResult(true));
            }

            blockchainApiClientProviderMock
                .Setup(x => x.GetApiClient(It.IsAny<string>()))
                .Returns(blockchainApiClient.Object);

            _logic = new BlackListService(repo, blockchainApiClientProviderMock.Object);
        }

        [Fact]
        public void IsBlockedAsync__AddedLowerCaseSensitiveCheck__False()
        {
            var model = SaveBlackListModel(BlockedAddressValid.ToLower(), true);
            var result = _logic.IsBlockedAsync(model.BlockchainType, BlockedAddressValid).Result;

            Assert.False(result);
        }

        [Fact]
        public void IsBlockedAsync__AddedLowerCaseNotSensitiveCheck__True()
        {
            var model = SaveBlackListModel(BlockedAddressValid.ToLower(), false);
            var result = _logic.IsBlockedAsync(model.BlockchainType, BlockedAddressValid).Result;

            Assert.True(result);
        }

        [Fact]
        public void IsBlockedAsync__AddedNormalCaseSensitiveCheck__True()
        {
            var model = SaveBlackListModel(BlockedAddressValid, true);
            var result = _logic.IsBlockedAsync(model.BlockchainType, BlockedAddressValid).Result;

            Assert.True(result);
        }

        [Fact]
        public void IsBlockedAsync__AddedNormalCaseNotSensitiveCheck__True()
        {
            var model = SaveBlackListModel(BlockedAddressValid, false);
            var result = _logic.IsBlockedAsync(model.BlockchainType, BlockedAddressValid).Result;

            Assert.True(result);
        }

        [Fact]
        public void IsBlockedAsync__NotBlocked__False()
        {
            var result = _logic.IsBlockedAsync(BlockchainType, BlockedAddressValid).Result;

            Assert.False(result);
        }

        [Fact]
        public void TryGetAsync__NotYetAdded__Null()
        {
            var blocked = _logic.TryGetAsync(BlockchainType, BlockedAddressValid).Result;

            Assert.Null(blocked);
        }

        [Fact]
        public void DeleteAsync__NotYetAdded__NoException()
        {
            _logic.DeleteAsync(BlockchainType, BlockedAddressValid).Wait();
        }

        [Fact]
        public void DeleteAsync__AddedBefore__Removed()
        {
            SaveBlackListModel(BlockedAddressValid, false);
            _logic.DeleteAsync(BlockchainType, BlockedAddressValid).Wait();

            var deleted = _logic.TryGetAsync(BlockchainType, BlockedAddressValid).Result;

            Assert.Null(deleted);
        }

        [Fact]
        public void SaveAsync__NotYetAdded__IsAdded()
        {
            AddBlackListAndCheck();
        }

        [Fact]
        public void TryGetAllAsync__NotYetAdded__NoException()
        {
            var result = _logic.TryGetAllAsync(BlockchainType, 100, null).Result;

            Assert.Equal(result.Item1.Count(), 0);
            Assert.Equal(result.continuationToken, null);
        }

        [Fact]
        public void TryGetAllAsync__AddedThree__RetreiveAll()
        {
            int count = 9;
            for (int i = 0; i < count; i++)
            {
                SaveBlackListModel($"0x{i}...", false);
            }
            
            var result = _logic.TryGetAllAsync(BlockchainType, count, null).Result;

            Assert.Equal(result.Item1.Count(), count);
            Assert.Equal(result.continuationToken, null);
        }

        [Fact]
        public void SaveAsync__AlreadyAdded__IsUpdated()
        {
            AddBlackListAndCheck();
            AddBlackListAndCheck();
        }

        #region Private

        private void AddBlackListAndCheck()
        {
            var model = SaveBlackListModel(BlockedAddressValid, true);

            var added = _logic.TryGetAsync(BlockchainType, BlockedAddressValid).Result;

            Assert.Equal(model.BlockedAddress, added.BlockedAddress);
             Assert.Equal(model.BlockchainType, added.BlockchainType);
            Assert.Equal(model.BlockedAddressLowCase, added.BlockedAddressLowCase);
            Assert.Equal(model.IsCaseSensitive, added.IsCaseSensitive);
        }

        private BlackListModel SaveBlackListModel(string blockedAddress, bool isCaseSensitiv)
        {
            var model = new BlackListModel(BlockchainType, blockedAddress, isCaseSensitiv);

            _logic.SaveAsync(model).Wait();
            return model;
        }

        #endregion
    }
}
