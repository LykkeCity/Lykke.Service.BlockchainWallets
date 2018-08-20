using Common;
using Common.Log;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;
using Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration;
using System;
using System.Threading.Tasks;
using Lykke.Bitcoin.Api.Client.BitcoinApi;
using Lykke.Common.Log;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.ClientAccount.Client;
using NBitcoin;

namespace Lykke.Service.BlockchainWallets.Services.FirstGeneration
{
    public class SrvBlockchainHelper : ISrvBlockchainHelper
    {
        private readonly IFirstGenerationBlockchainWalletRepository _walletCredentialsRepository;
        private readonly ILog _log;
        private readonly IClientAccountClient _clientAccountService;
        private readonly IBitcoinApiClient _bitcoinApiClient;
        private readonly BitcoinCoreSettings _btcSettings;

        public SrvBlockchainHelper(IFirstGenerationBlockchainWalletRepository walletCredentialsRepository,
            ILogFactory logFactory,
            IClientAccountClient clientAccountService,
            IBitcoinApiClient bitcoinApiClient,
            BitcoinCoreSettings btcSettings)
        {
            if (logFactory == null)
                throw new ArgumentNullException(nameof(logFactory));
            _log = logFactory.CreateLog(this);

            _walletCredentialsRepository = walletCredentialsRepository ?? throw new ArgumentNullException(nameof(walletCredentialsRepository));
            _clientAccountService = clientAccountService ?? throw new ArgumentNullException(nameof(clientAccountService));
            _bitcoinApiClient = bitcoinApiClient ?? throw new ArgumentNullException(nameof(bitcoinApiClient));
            _btcSettings = btcSettings ?? throw new ArgumentNullException(nameof(btcSettings));
        }

        public async Task<IBcnCredentialsRecord> GenerateWallets(Guid clientId)
        {
            var network = _btcSettings.NetworkType == NetworkType.Main ? Network.Main : Network.TestNet;

            var wallets = await GetWalletsForPubKey();
            IBcnCredentialsRecord bcnCreds;
            var currentWalletCreds = await _walletCredentialsRepository.GetAsync(clientId);

            if (currentWalletCreds == null)
            {
                var btcConvertionWallet = GetNewAddressAndPrivateKey(network);

                IWalletCredentials walletCreds = WalletCredentials.Create(
                    clientId.ToString(), 
                    null, 
                    null, 
                    null,
                    wallets.ColoredMultiSigAddress,
                    btcConvertionWallet.PrivateKey, 
                    btcConvertionWallet.Address, 
                    encodedPk: "",
                    pubKey: "");

                bcnCreds = BcnCredentialsRecord.Create(SpecialAssetIds.BitcoinAssetId,
                    clientId.ToString(),
                    null,
                    wallets.SegwitAddress,
                    "");

                await Task.WhenAll(
                    _walletCredentialsRepository.SaveAsync(walletCreds),
                    _walletCredentialsRepository.SaveAsync(bcnCreds)
                );
            }
            else
            {
                //walletCreds = WalletCredentials.Create(
                //    clientId.ToString(), 
                //    null, 
                //    null, 
                //    null,
                //    wallets.ColoredMultiSigAddress, 
                //    null, 
                //    null, 
                //    encodedPk: "",
                //    pubKey: "");

                bcnCreds = await _walletCredentialsRepository.GetBcnCredsAsync(SpecialAssetIds.BitcoinAssetId,
                    clientId);
                if (bcnCreds == null)
                {
                    bcnCreds = BcnCredentialsRecord.Create(
                        SpecialAssetIds.BitcoinAssetId,
                        clientId.ToString(),
                        null,
                        wallets.SegwitAddress,
                        ""
                    );

                    await _walletCredentialsRepository.SaveAsync(bcnCreds);
                }

                //await _walletCredentialsHistoryRepository.InsertHistoryRecord(currentWalletCreds);
                //await _walletCredentialsRepository.MergeAsync(walletCreds);
            }

            return bcnCreds;
        }

        private async Task SetDefaultRefundAddress(string clientId, string coloredAddressWif)
        {
            var refundSettings = await _clientAccountService.GetRefundAddressAsync(clientId);
            if (string.IsNullOrEmpty(refundSettings.Address))
            {
                await _clientAccountService.SetRefundAddressAsync(clientId, coloredAddressWif, 30/*Days*/, true);
            }
        }

        #region Tools

        private async Task<GetWalletResponse> GetWalletsForPubKey()
        {
            try
            {
                var segwitResponse = await _bitcoinApiClient.GetSegwitWallet();

                if (segwitResponse.HasError)
                    throw new Exception($"Bad response from Bitcoin, error: {segwitResponse.Error.ToJson()}");

                return new GetWalletResponse
                {
                    ColoredMultiSigAddress = segwitResponse.ColoredAddress,
                    SegwitAddress = segwitResponse.Address
                };
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                throw;
            }
        }

        class WalletKeyAndAddress
        {
            public string PrivateKey { get; set; }
            public string Address { get; set; }
        }

        private static WalletKeyAndAddress GetNewAddressAndPrivateKey(Network network)
        {
            var key = new Key();
            var secret = new BitcoinSecret(key, network);

            var walletAddress = secret.GetAddress().ToWif();
            var walletPrivateKey = secret.PrivateKey.GetWif(network).ToWif();

            return new WalletKeyAndAddress
            {
                Address = walletAddress,
                PrivateKey = walletPrivateKey
            };
        }

        #endregion

        #region WalletBackend response models

        internal class GetWalletResponse
        {
            public string ColoredMultiSigAddress { get; set; }
            public string SegwitAddress { get; set; }
        }

        #endregion
    }
}
