using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Common.Log;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeSlack;
using Lykke.Service.BlockchainWallets.Core.Settings;
using Lykke.Service.BlockchainWallets.Modules;
using Lykke.SettingsReader;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;

namespace Lykke.Service.BlockchainWallets
{
    [UsedImplicitly]
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        private IContainer ApplicationContainer { get; set; }

        private IConfigurationRoot Configuration { get; }

        private IHealthNotifier HealthNotifier { get; set; }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseLykkeMiddleware(ex => new { Message = "Technical problem" });

            app.UseMvc();
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swagger, httpReq) => swagger.Host = httpReq.Host.Value);
            });
            app.UseSwaggerUI(x =>
            {
                x.RoutePrefix = "swagger/ui";
                x.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            });
            app.UseStaticFiles();

            appLifetime.ApplicationStarted.Register(StartApplication);
            appLifetime.ApplicationStopped.Register(CleanUp);
        }

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver =
                        new DefaultContractResolver();
                });

            services.AddSwaggerGen(options =>
            {
                options.DefaultLykkeConfiguration("v1", "BlockchainWallets API");
            });

            var builder = new ContainerBuilder();
            var appSettings = Configuration.LoadSettings<AppSettings>();
            var slackSettings = appSettings.CurrentValue.SlackNotifications;
            Configuration.CheckDependenciesAsync(
                appSettings,
                slackSettings.AzureQueue.ConnectionString,
                slackSettings.AzureQueue.QueueName,
                "BlockchainWallets");

            services.AddLykkeLogging(
                appSettings.ConnectionString(x => x.BlockchainWalletsService.Db.LogsConnString),
                "BlockchainWalletsLog",
                slackSettings.AzureQueue.ConnectionString,
                slackSettings.AzureQueue.QueueName,
                logging => 
                {
                    logging.AddAdditionalSlackChannel("BlockChainIntegration");
                    logging.AddAdditionalSlackChannel("BlockChainIntegrationImportantMessages", options =>
                    {
                        options.MinLogLevel = Microsoft.Extensions.Logging.LogLevel.Warning;
                    });
                }
            );

            builder
            .RegisterModule(new CqrsModule(appSettings.CurrentValue.BlockchainWalletsService.Cqrs))
            .RegisterModule(new RepositoriesModule(appSettings.Nested(x => x.BlockchainWalletsService.Db)))
            .RegisterModule(new ServiceModule(
                appSettings.CurrentValue.BlockchainsIntegration,
                appSettings.CurrentValue.BlockchainSignFacadeClient,
                appSettings.CurrentValue,
                appSettings.CurrentValue.AssetsServiceClient));

            builder.Populate(services);

            ApplicationContainer = builder.Build();

            HealthNotifier = ApplicationContainer.Resolve<IHealthNotifier>();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        private void CleanUp()
        {            
            // NOTE: Service can't recieve and process requests here, so you can destroy all resources

            HealthNotifier?.Notify("Terminating");

            ApplicationContainer.Dispose();
        }

        private void StartApplication()
        {
            HealthNotifier?.Notify("Started");
        }
    }
}
