using Autofac;
using Common.Log;
using Lykke.Service.BlockchainSignFacade.Client;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Core.Settings.BlockchainIntegrationSettings;
using Lykke.Service.BlockchainWallets.Core.Settings.BlockchainSignFacadeClient;
using Lykke.Service.BlockchainWallets.Core.Settings.ServiceSettings;
using Lykke.Service.BlockchainWallets.Services;

namespace Lykke.Service.BlockchainWallets.Modules
{
    public class ServiceModule : Module
    {
        private readonly BlockchainsIntegrationSettings _blockchainsIntegrationSettings;
        private readonly BlockchainSignFacadeClientSettings _blockchainSignFacadeClientSettings;
        private readonly BlockchainWalletsSettings _blockchainWalletsSettings;
        private readonly ILog _log;


        public ServiceModule(

            BlockchainsIntegrationSettings blockchainsIntegrationSettings,
            BlockchainSignFacadeClientSettings blockchainSignFacadeClientSettings,
            BlockchainWalletsSettings blockchainWalletsSettings,
            ILog log)
        {
            _blockchainsIntegrationSettings = blockchainsIntegrationSettings;
            _blockchainSignFacadeClientSettings = blockchainSignFacadeClientSettings;
            _blockchainWalletsSettings = blockchainWalletsSettings;
            _log = log;
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
                .RegisterType<StartupManager>()
                .As<IStartupManager>()
                .SingleInstance();

            builder
                .RegisterType<ShutdownManager>()
                .As<IShutdownManager>()
                .SingleInstance();

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
