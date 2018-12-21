using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Lykke.Common.Log;
using Lykke.Service.BlockchainWallets.AzureRepositories;
using Lykke.Service.BlockchainWallets.AzureRepositories.FirstGeneration;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Core.Repositories.FirstGeneration;
using Lykke.Service.BlockchainWallets.Core.Settings.ServiceSettings;
using Lykke.SettingsReader;

namespace Lykke.Service.BlockchainWallets.MigrateAdditionalWalletsIndexes
{
    public class CustomRepositoryModel : Module
    {
        private readonly IReloadingManager<DbSettings> _dbSettings;

        public CustomRepositoryModel(IReloadingManager<DbSettings> dbSettings)
        {
            _dbSettings = dbSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var connectionString = _dbSettings.Nested(x => x.DataConnString);

            builder
                .Register(c => WalletRepository.Create(connectionString, c.Resolve<ILogFactory>()))
                .As<IWalletRepository>()
                .SingleInstance();

            builder
                .Register(c => BlockchainWalletsRepository.Create(connectionString, c.Resolve<ILogFactory>()))
                .As<IBlockchainWalletsRepository>()
                .SingleInstance();

            builder
                .Register(c => AdditionalWalletRepository.Create(connectionString, c.Resolve<ILogFactory>()))
                .As<IAdditionalWalletRepository>()
                .SingleInstance();

            builder
                .Register(c =>
                    FirstGenerationBlockchainWalletRepository.Create(
                        _dbSettings.Nested(x => x.ClientPersonalInfoConnString), c.Resolve<ILogFactory>()))
                .As<IFirstGenerationBlockchainWalletRepository>()
                .SingleInstance();

            builder
                .Register(c => MonitoringSubscriptionRepository.Create(connectionString, c.Resolve<ILogFactory>()))
                .As<IMonitoringSubscriptionRepository>()
                .SingleInstance();

            builder
                .Register(c =>
                    WalletCredentialsHistoryRepository.Create(_dbSettings.Nested(x => x.ClientPersonalInfoConnString),
                        c.Resolve<ILogFactory>()))
                .As<IWalletCredentialsHistoryRepository>()
                .SingleInstance();
        }
    }
}
