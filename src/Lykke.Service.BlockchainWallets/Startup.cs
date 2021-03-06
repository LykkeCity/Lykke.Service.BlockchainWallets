﻿using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Common.Log;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeSlack;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Core.Settings;
using Lykke.Service.BlockchainWallets.Modules;
using Lykke.SettingsReader;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using System;
using Lykke.Common;

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

        private ILog Log { get; set; }

        private IHealthNotifier HealthNotifier { get; set; }

        private void FatalErrorStdOut(Exception ex)
        {
            Console.WriteLine($"FATAL ERROR: {DateTime.UtcNow} : Startup : {ex}");
        }


        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            try
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                app.UseLykkeMiddleware(ex => Common.Api.Contract.Responses.ErrorResponse.Create(ex.Message));

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
            catch (Exception ex)
            {
                if (Log != null)
                    Log.Critical(ex);
                else
                    FatalErrorStdOut(ex);

                throw;
            }
        }

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            try
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
                var appSettings = Configuration.LoadSettings<AppSettings>(o =>
                {
                    o.SetConnString(s => s.SlackNotifications.AzureQueue.ConnectionString);
                    o.SetQueueName(s => s.SlackNotifications.AzureQueue.QueueName);
                    o.SenderName = $"{AppEnvironment.Name} {AppEnvironment.Version}";
                });

                var slackSettings = appSettings.CurrentValue.SlackNotifications;


                services.AddLykkeLogging(
                    appSettings.ConnectionString(x => x.BlockchainWalletsService.Db.LogsConnString),
                    "BlockchainWalletsLog",
                    slackSettings.AzureQueue.ConnectionString,
                    slackSettings.AzureQueue.QueueName,
                    logging =>
                    {
                        logging.AddAdditionalSlackChannel("CommonBlockChainIntegration");
                        logging.AddAdditionalSlackChannel("CommonBlockChainIntegrationImportantMessages", options =>
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
                    appSettings.CurrentValue.AssetsServiceClient,
                    appSettings.CurrentValue.BlockchainWalletsService));

                builder.Populate(services);

                ApplicationContainer = builder.Build();

                Log = ApplicationContainer.Resolve<ILogFactory>().CreateLog(this);
                HealthNotifier = ApplicationContainer.Resolve<IHealthNotifier>();

                return new AutofacServiceProvider(ApplicationContainer);
            }
            catch (Exception ex)
            {
                if (Log != null)
                    Log.Critical(ex);
                else
                    FatalErrorStdOut(ex);

                throw;
            }
        }

        private void CleanUp()
        {
            try
            {
                // NOTE: Service can't recieve and process requests here, so you can destroy all resources

                HealthNotifier?.Notify("Terminating");

                ApplicationContainer.Dispose();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    Log.Critical(ex);
                    (Log as IDisposable)?.Dispose();
                }
                else
                {
                    FatalErrorStdOut(ex);
                }

                throw;
            }
        }

        private void StartApplication()
        {
            try
            {
                ApplicationContainer.Resolve<IStartupManager>().Start();
                
                HealthNotifier?.Notify("Started");
            }
            catch (Exception ex)
            {
                if (Log != null)
                    Log.Critical(ex);
                else
                    FatalErrorStdOut(ex);

                throw;
            }
        }
    }
}
