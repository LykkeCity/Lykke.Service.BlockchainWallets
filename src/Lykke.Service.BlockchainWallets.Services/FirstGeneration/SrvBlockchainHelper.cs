using Common;
using Common.Log;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;
using Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration;
using System;
using System.Threading.Tasks;
using Lykke.Bitcoin.Api.Client.BitcoinApi;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Core.Repositories.FirstGeneration;
using Lykke.Service.ClientAccount.Client;
using NBitcoin;

namespace Lykke.Service.BlockchainWallets.Services.FirstGeneration
{
    public class SrvBlockchainHelper : ISrvBlockchainHelper
    {
        private readonly IFirstGenerationBlockchainWalletRepository _walletCredentialsRepository;
        private readonly ILog _log;
        private readonly IWalletCredentialsHistoryRepository _walletCredentialsHistoryRepository;
        private readonly IClientAccountClient _clientAccountService;
        private readonly IBitcoinApiClient _bitcoinApiClient;

        public SrvBlockchainHelper(IFirstGenerationBlockchainWalletRepository walletCredentialsRepository,
            ILog log,
            IWalletCredentialsHistoryRepository walletCredentialsHistoryRepository,
            IClientAccountClient clientAccountService,
            IBitcoinApiClient bitcoinApiClient)
        {
            _walletCredentialsRepository = walletCredentialsRepository;
            _log = log;
            _walletCredentialsHistoryRepository = walletCredentialsHistoryRepository;
            _clientAccountService = clientAccountService;
            _bitcoinApiClient = bitcoinApiClient;
        }

        public async Task<IWalletCredentials> GenerateWallets(string clientId, string clientPubKeyHex, string encodedPrivateKey, NetworkType networkType)
        {
            var network = networkType == NetworkType.Main ? Network.Main : Network.TestNet;

            PubKey clientPubKey = new PubKey(clientPubKeyHex);
            var clientAddress = clientPubKey.GetAddress(network);
            var clientAddressWif = clientAddress.ToWif();
            var coloredAddressWif = clientAddress.ToColoredAddress().ToWif();

            var wallets = await GetWalletsForPubKey(clientPubKeyHex);

            var currentWalletCreds = await _walletCredentialsRepository.GetAsync(Guid.Parse(clientId));

            IWalletCredentials walletCreds;
            if (currentWalletCreds == null)
            {
                var btcConvertionWallet = GetNewAddressAndPrivateKey(network);

                walletCreds = WalletCredentials.Create(
                    clientId, clientAddressWif, null, wallets.MultiSigAddress,
                    wallets.ColoredMultiSigAddress,
                    btcConvertionWallet.PrivateKey, btcConvertionWallet.Address, encodedPk: encodedPrivateKey,
                    pubKey: clientPubKeyHex);

                await Task.WhenAll(
                    _walletCredentialsRepository.SaveAsync(walletCreds),
                    _walletCredentialsRepository.SaveAsync(BcnCredentialsRecord.Create(SpecialAssetIds.BitcoinAssetId, 
                        clientId, 
                        null, 
                        wallets.SegwitAddress, 
                        clientPubKeyHex))
                );
            }
            else
            {
                walletCreds = WalletCredentials.Create(
                    clientId, clientAddressWif, null, wallets.MultiSigAddress,
                    wallets.ColoredMultiSigAddress, null, null, encodedPk: encodedPrivateKey,
                    pubKey: clientPubKeyHex);

                if (await _walletCredentialsRepository.GetBcnCredsAsync(SpecialAssetIds.BitcoinAssetId, Guid.Parse(clientId)) == null)
                    await _walletCredentialsRepository.SaveAsync(BcnCredentialsRecord.Create(
                        SpecialAssetIds.BitcoinAssetId, clientId, null, wallets.SegwitAddress,
                        clientPubKeyHex));

                await _walletCredentialsHistoryRepository.InsertHistoryRecord(currentWalletCreds);
                await _walletCredentialsRepository.MergeAsync(walletCreds);
            }

            await SetDefaultRefundAddress(clientId, coloredAddressWif);

            return walletCreds;
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

        private async Task<GetWalletResponse> GetWalletsForPubKey(string pubKeyHex)
        {
            try
            {
                var response = await _bitcoinApiClient.GetWallet(pubKeyHex);

                var segwitResponse = await _bitcoinApiClient.GetSegwitWallet(pubKeyHex);

                if (response.HasError || segwitResponse.HasError)
                    throw new Exception($"Bad response from Bitcoin, error: {response.Error.ToJson()}");

                return new GetWalletResponse
                {
                    ColoredMultiSigAddress = response.ColoredMultisig,
                    MultiSigAddress = response.Multisig,
                    SegwitAddress = segwitResponse.Address
                };
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync("SrvBlockchainHelper", "GenerateTransferTransaction", pubKeyHex, ex);
                throw;
            }
        }

        class WalletKeyAndAddress
        {
            public string PrivateKey { get; set; }
            public string Address { get; set; }
        }

        private WalletKeyAndAddress GetNewAddressAndPrivateKey(Network network)
        {
            Key key = new Key();
            BitcoinSecret secret = new BitcoinSecret(key, network);

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
            public string MultiSigAddress { get; set; }
            public string ColoredMultiSigAddress { get; set; }
            public string SegwitAddress { get; set; }
        }

        #endregion
    }
}
