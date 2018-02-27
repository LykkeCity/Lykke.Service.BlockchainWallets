using Autofac;
using Common.Log;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Core.Settings.BlockchainIntegrationSettings;
using Lykke.Service.BlockchainWallets.Core.Settings.ServiceSettings;
using Lykke.Service.BlockchainWallets.Services;

namespace Lykke.Service.BlockchainWallets.Modules
{
    public class ServiceModule : Module
    {
        private readonly BlockchainWalletsSettings _blockchainWalletsSettings;
        private readonly BlockchainsIntegrationSettings _blockchainsIntegrationSettings;
        private readonly ILog _log;


        public ServiceModule(
            BlockchainWalletsSettings blockchainWalletsSettings,
            BlockchainsIntegrationSettings blockchainsIntegrationSettings,
            ILog log)
        {
            _blockchainWalletsSettings = blockchainWalletsSettings;
            _blockchainsIntegrationSettings = blockchainsIntegrationSettings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder
                .Register(ctx => _blockchainWalletsSettings)
                .AsSelf()
                .SingleInstance();

            builder
                .Register(ctx => _blockchainsIntegrationSettings)
                .AsSelf()
                .SingleInstance();

            builder
                .RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

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
        }
    }
}
