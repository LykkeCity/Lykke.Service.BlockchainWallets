using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
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
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;

namespace Lykke.Service.BlockchainWallets.Modules
{
    public class ServiceModule : Module
    {
        private readonly BlockchainsIntegrationSettings _blockchainsIntegrationSettings;
        private readonly BlockchainSignFacadeClientSettings _blockchainSignFacadeClientSettings;
        private readonly AppSettings _appSettings;
        private readonly AssetServiceClientSettings _assetServiceSettings;
        private readonly ILog _log;
        private readonly IServiceCollection _services;

        public ServiceModule(
            BlockchainsIntegrationSettings blockchainsIntegrationSettings,
            BlockchainSignFacadeClientSettings blockchainSignFacadeClientSettings,
            AppSettings appSettings,
            AssetServiceClientSettings assetServiceSettings,
            ILog log)
        {
            _blockchainsIntegrationSettings = blockchainsIntegrationSettings;
            _blockchainSignFacadeClientSettings = blockchainSignFacadeClientSettings;
            _appSettings = appSettings;
            _assetServiceSettings = assetServiceSettings;
            _log = log;
            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder
                .Register(ctx => _blockchainsIntegrationSettings)
                .AsSelf()
                .SingleInstance();
            
            builder
                .RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();
            
            builder
                .RegisterInstance(CreateBlockchainSignFacadeClient())
                .As<IBlockchainSignFacadeClient>();

            builder
                .RegisterType<BlockchainIntegrationService>()
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
                .RegisterType<CapabilitiesService>()
                .As<ICapabilitiesService>()
                .SingleInstance();

            builder
                .RegisterType<ConstantsService>()
                .As<IConstantsService>()
                .SingleInstance();

            builder
                .RegisterType<AddressParser>()
                .As<IAddressParser>();
            
            _services.RegisterAssetsClient(AssetServiceSettings.Create(
                new Uri(_assetServiceSettings.ServiceUrl),
                _assetServiceSettings.ExpirationPeriod), _log);

            #region FirstGenerationServices

            builder
                .RegisterType<ChronoBankService>()
                .As<IChronoBankService>();

            builder
                .RegisterType<LegacyWalletService>()
                .As<ILegacyWalletService>();

            builder
                .RegisterType<QuantaService>()
                .As<IQuantaService>();

            builder
                .RegisterType<SrvBlockchainHelper>()
                .As<ISrvBlockchainHelper>();

            builder
                .RegisterType<SrvEthereumHelper>()
                .As<ISrvEthereumHelper>();

            builder
                .RegisterType<SrvSolarCoinHelper>()
                .As<ISrvSolarCoinHelper>();

            builder.RegisterInstance<IBitcoinApiClient>(new BitcoinApiClient(_appSettings.BitcoinCoreSettings.BitcoinCoreApiUrl));
            builder.RegisterLykkeServiceClient(_appSettings.ClientAccountServiceClient.ServiceUrl);
            builder.RegisterInstance<IEthereumCoreAPI>(new EthereumCoreAPI(new Uri(_appSettings.EthereumServiceClient.ServiceUrl), new HttpClient()));
            builder.RegisterInstance<SolarCoinServiceClientSettings>(_appSettings.SolarCoinServiceClientSettings);

            #endregion

            builder.Populate(_services);
        }

        private IBlockchainSignFacadeClient CreateBlockchainSignFacadeClient()
        {
            return new BlockchainSignFacadeClient
            (
                hostUrl: _blockchainSignFacadeClientSettings.ServiceUrl,
                apiKey: _appSettings.BlockchainWalletsService.SignFacadeApiKey,
                log: _log
            );
        }
    }
}
