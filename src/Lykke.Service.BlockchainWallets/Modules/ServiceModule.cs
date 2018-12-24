using Autofac;
using Lykke.Bitcoin.Api.Client.BitcoinApi;
using Lykke.Service.Assets.Client;
using Lykke.Service.BlockchainSignFacade.Client;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Core.Services.FirstGeneration;
using Lykke.Service.BlockchainWallets.Core.Settings;
using Lykke.Service.BlockchainWallets.Core.Settings.BlockchainIntegrationSettings;
using Lykke.Service.BlockchainWallets.Core.Settings.BlockchainSignFacadeClient;
using Lykke.Service.BlockchainWallets.Services;
using Lykke.Service.BlockchainWallets.Services.FirstGeneration;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.EthereumCore.Client;
using System;
using System.Net.Http;
using Lykke.Common.Log;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;
using Lykke.Service.BlockchainWallets.Core.Settings.ServiceSettings;

namespace Lykke.Service.BlockchainWallets.Modules
{
    public class ServiceModule : Module
    {
        private readonly BlockchainsIntegrationSettings _blockchainsIntegrationSettings;
        private readonly BlockchainSignFacadeClientSettings _blockchainSignFacadeClientSettings;
        private readonly AppSettings _appSettings;
        private readonly AssetServiceClientSettings _assetServiceSettings;
        private readonly BlockchainWalletsSettings _blockchainWalletsSettings;

        public ServiceModule(
            BlockchainsIntegrationSettings blockchainsIntegrationSettings,
            BlockchainSignFacadeClientSettings blockchainSignFacadeClientSettings,
            AppSettings appSettings,
            AssetServiceClientSettings assetServiceSettings,
            BlockchainWalletsSettings blockchainWalletsSettings)
        {
            _blockchainsIntegrationSettings = blockchainsIntegrationSettings;
            _blockchainSignFacadeClientSettings = blockchainSignFacadeClientSettings;
            _appSettings = appSettings;
            _assetServiceSettings = assetServiceSettings;
            _blockchainWalletsSettings = blockchainWalletsSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder
                .Register(ctx => _blockchainsIntegrationSettings)
                .AsSelf()
                .SingleInstance();
            
            builder
                .Register(CreateBlockchainSignFacadeClient)
                .As<IBlockchainSignFacadeClient>();

            builder
                .RegisterType<BlockchainIntegrationService>()
                .WithParameter("timeoutFoApiInSeconds", _blockchainWalletsSettings.BlockchainApiTimeoutInSeconds)
                .As<IBlockchainIntegrationService>()
                .SingleInstance();

            builder
                .RegisterType<HealthService>()
                .As<IHealthService>();

            builder
                .RegisterType<WalletService>()
                .As<IWalletService>();
            
            builder
                .RegisterType<AddressService>()
                .As<IAddressService>()
                .SingleInstance();

            builder
                .RegisterType<BlockchainExtensionsService>()
                .As<IBlockchainExtensionsService>()
                .SingleInstance();

            builder
                .RegisterType<BlockchainAssetService>()
                .As<IBlockchainAssetService>()
                .SingleInstance();

            builder
                .RegisterType<AddressParser>()
                .As<IAddressParser>();

            builder
                .RegisterAssetsClient(AssetServiceSettings.Create(
                    new Uri(_assetServiceSettings.ServiceUrl),
                    _assetServiceSettings.ExpirationPeriod));

            builder
                .RegisterType<StartupManager>()
                .As<IStartupManager>();

            #region FirstGenerationServices

            builder
                .RegisterType<LegacyWalletService>()
                .As<ILegacyWalletService>();

            builder
                .RegisterType<SrvBlockchainHelper>()
                .As<ISrvBlockchainHelper>();

            builder
                .RegisterType<SrvEthereumHelper>()
                .As<ISrvEthereumHelper>();

            builder
                .RegisterType<SrvSolarCoinHelper>()
                .As<ISrvSolarCoinHelper>();

            builder.RegisterInstance<BitcoinCoreSettings>(_appSettings.BitcoinCoreSettings);
            builder.RegisterInstance<IBitcoinApiClient>(new BitcoinApiClient(_appSettings.BitcoinCoreSettings.BitcoinCoreApiUrl));
            builder.RegisterLykkeServiceClient(_appSettings.ClientAccountServiceClient.ServiceUrl);
            builder.RegisterInstance<IEthereumCoreAPI>(new EthereumCoreAPI(new Uri(_appSettings.EthereumServiceClient.ServiceUrl), new HttpClient()));
            builder.RegisterInstance<SolarCoinServiceClientSettings>(_appSettings.SolarCoinServiceClient);
            builder.RegisterInstance<QuantaServiceClientSettings>(_appSettings.QuantaServiceClient);
            builder.RegisterInstance<ChronoBankServiceClientSettings>(_appSettings.ChronoBankServiceClient);

            #endregion
        }

        private IBlockchainSignFacadeClient CreateBlockchainSignFacadeClient(IComponentContext ctx)
        {
            return new BlockchainSignFacadeClient
            (
                hostUrl: _blockchainSignFacadeClientSettings.ServiceUrl,
                apiKey: _appSettings.BlockchainWalletsService.SignFacadeApiKey,
                log: ctx.Resolve<ILogFactory>().CreateLog(nameof(BlockchainSignFacadeClient))
            );
        }
    }
}
