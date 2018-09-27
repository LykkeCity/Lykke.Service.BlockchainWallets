using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Common.Log;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeSlack;
using Lykke.Service.BlockchainWallets.Core.Settings;
using Lykke.Service.BlockchainWallets.CTests.Utils;
using Lykke.Service.BlockchainWallets.Modules;
using Lykke.SettingsReader;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;

namespace Lykke.Service.BlockchainWallets.CTests.Common
{
    public class CustomWebApplicationFactory<TStartup>
        : WebApplicationFactory<Startup>
    {
        private ILog Log;
        private readonly LaunchSettingsFixture _fixture;

        public CustomWebApplicationFactory()
        {
            _fixture = new LaunchSettingsFixture();
            var builder = new ConfigurationBuilder()
                .SetBasePath(@"E:\LykkeCity\BlockchainIntegration\BlockchainWallets\src\Lykke.Service.BlockchainWallets\bin\Debug\netcoreapp2.1")
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:5000")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseApplicationInsights();

            return builder;
        }

        //protected override void ConfigureWebHost(IWebHostBuilder builder)
        //{
        //    builder.ConfigureServices(services =>
        //    {
        //        services.AddMvc()
        //            .AddJsonOptions(options =>
        //            {
        //                options.SerializerSettings.ContractResolver =
        //                    new DefaultContractResolver();
        //            });

        //        services.AddSwaggerGen(options =>
        //        {
        //            options.DefaultLykkeConfiguration("v1", "BlockchainWallets API");
        //        });

        //        var containerBuilder = new ContainerBuilder();
        //        var appSettings = Configuration.LoadSettings<AppSettings>();
        //        var slackSettings = appSettings.CurrentValue.SlackNotifications;
        //        Configuration.CheckDependenciesAsync(
        //            appSettings,
        //            slackSettings.AzureQueue.ConnectionString,
        //            slackSettings.AzureQueue.QueueName,
        //            "BlockchainWallets");

        //        services.AddLykkeLogging(
        //            appSettings.ConnectionString(x => x.BlockchainWalletsService.Db.LogsConnString),
        //            "BlockchainWalletsLog",
        //            slackSettings.AzureQueue.ConnectionString,
        //            slackSettings.AzureQueue.QueueName,
        //            logging =>
        //            {
        //                logging.AddAdditionalSlackChannel("CommonBlockChainIntegration");
        //                logging.AddAdditionalSlackChannel("CommonBlockChainIntegrationImportantMessages", options =>
        //                {
        //                    options.MinLogLevel = Microsoft.Extensions.Logging.LogLevel.Warning;
        //                });
        //            }
        //        );

        //        containerBuilder
        //        .RegisterModule(new CqrsModule(appSettings.CurrentValue.BlockchainWalletsService.Cqrs))
        //        .RegisterModule(new RepositoriesModule(appSettings.Nested(x => x.BlockchainWalletsService.Db)))
        //        .RegisterModule(new ServiceModule(
        //            appSettings.CurrentValue.BlockchainsIntegration,
        //            appSettings.CurrentValue.BlockchainSignFacadeClient,
        //            appSettings.CurrentValue,
        //            appSettings.CurrentValue.AssetsServiceClient));

        //        containerBuilder.Populate(services);

        //        ApplicationContainer = containerBuilder.Build();

        //        Log = ApplicationContainer.Resolve<ILogFactory>().CreateLog(this);

        //    });
        //}

        //public IContainer ApplicationContainer { get; set; }
    }
}
