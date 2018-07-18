using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Service.Assets.Client;
using Lykke.Service.BlockchainSignFacade.Client;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Core.Settings;
using Lykke.Service.BlockchainWallets.Core.Settings.BlockchainIntegrationSettings;
using Lykke.Service.BlockchainWallets.Core.Settings.BlockchainSignFacadeClient;
using Lykke.Service.BlockchainWallets.Core.Settings.ServiceSettings;
using Lykke.Service.BlockchainWallets.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.BlockchainWallets.Modules
{
    public class ServiceModule : Module
    {
        private readonly BlockchainsIntegrationSettings _blockchainsIntegrationSettings;
        private readonly BlockchainSignFacadeClientSettings _blockchainSignFacadeClientSettings;
        private readonly BlockchainWalletsSettings _blockchainWalletsSettings;
        private readonly AssetServiceClientSettings _assetServiceSettings;
        private readonly ILog _log;
        private readonly IServiceCollection _services;

        public ServiceModule(
            BlockchainsIntegrationSettings blockchainsIntegrationSettings,
            BlockchainSignFacadeClientSettings blockchainSignFacadeClientSettings,
            BlockchainWalletsSettings blockchainWalletsSettings,
            AssetServiceClientSettings assetServiceSettings,
            ILog log)
        {
            _blockchainsIntegrationSettings = blockchainsIntegrationSettings;
            _blockchainSignFacadeClientSettings = blockchainSignFacadeClientSettings;
            _blockchainWalletsSettings = blockchainWalletsSettings;
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
            
            builder.Populate(_services);
        }

        private IBlockchainSignFacadeClient CreateBlockchainSignFacadeClient()
        {
            return new BlockchainSignFacadeClient
            (
                hostUrl: _blockchainSignFacadeClientSettings.ServiceUrl,
                apiKey: _blockchainWalletsSettings.SignFacadeApiKey,
                log: _log
            );
        }
    }
}
