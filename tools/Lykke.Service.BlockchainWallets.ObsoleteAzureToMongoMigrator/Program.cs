using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.Settings;
using Lykke.Service.BlockchainWallets.MongoRepositories.Wallets;
using Lykke.Service.BlockchainWallets.ObsoleteAzureToMongoMigrator.Cqrs;
using Lykke.Service.BlockchainWallets.ObsoleteAzureToMongoMigrator.ObsoleteAzurePepo;
using Lykke.Service.BlockchainWallets.Workflow.Commands;
using Lykke.SettingsReader;
using Microsoft.Extensions.CommandLineUtils;

namespace Lykke.Service.BlockchainWallets.ObsoleteAzureToMongoMigrator
{
    class Program
    {
        private const string BwSettingsUrl = "-BWSettingsUrl | -BW";

        private static void Main(string[] args)
        {
            var application = new CommandLineApplication
            {
                Description = "Migrates db structure from obsolete azure to mongo"
            };

            var arguments = new Dictionary<string, CommandArgument>
            {

                {BwSettingsUrl, application.Argument(BwSettingsUrl, "Blockchain wallets settings url")},
            };

            application.HelpOption("-? | -h | --help");
            application.OnExecute(async () =>
            {
                try
                {
                    if (arguments.Any(x => string.IsNullOrEmpty(x.Value.Value)))
                    {
                        application.ShowHelp();
                    }
                    else
                    {
                        await Execute(arguments[BwSettingsUrl].Value);

                        Console.WriteLine("All done");
                    }

                    return 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e);

                    return 1;
                }
            });


            application.Execute(args);
        }


        private static async Task Execute(string settingsUrl)
        {
            var appSettings = new SettingsServiceReloadingManager<AppSettings>(settingsUrl, p => { });
            var logfactory = LogFactory.Create().AddConsole();

            var walletMongoRepo = BlockchainWalletMongoRepository.Create(
                appSettings.CurrentValue.BlockchainWalletsService.Db.Mongo.ConnString,
                appSettings.CurrentValue.BlockchainWalletsService.Db.Mongo.DbName,
                logfactory);

            var obsoleteRepo = BlockchainWalletsRepository.Create(appSettings.Nested(p => p.BlockchainWalletsService.Db.DataConnString), logfactory);

            var cqrsEngine =
                CqrsBuilder.CreateCqrsEngine(
                    appSettings.CurrentValue.BlockchainWalletsService.Cqrs.RabbitConnectionString, logfactory);

            cqrsEngine.Start();

            Console.WriteLine($"[{DateTime.UtcNow}] Ensuring indexes created");
            await walletMongoRepo.EnsureIndexesCreatedAsync();

            const int take = 1000;
            string continuationToken = null;

            var counter = 0;

            var throttler = new SemaphoreSlim(8);
            var tasks = new List<Task>();

            var disrupt = new CancellationTokenSource();
            do
            {

                await throttler.WaitAsync(disrupt.Token);
                var queryResult = await obsoleteRepo.GetAllAsync(take, continuationToken);

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var insInMongo = walletMongoRepo.InsertBatchAsync(queryResult.Wallets.Select(p =>
                            (blockchainType: p.wallet.BlockchainType, clientId: p.wallet.ClientId,
                                address: p.wallet.Address,
                                createdBy: p.wallet.CreatorType, addAsLatest: p.isPrimary)));

                        foreach (var item in queryResult.Wallets)
                        {
                            cqrsEngine.SendCommand(new CreateWalletBackupCommand
                                {
                                    ClientId = item.wallet.ClientId,
                                    Address = item.wallet.Address,
                                    AssetId = item.wallet.AssetId,
                                    BlockchainType = item.wallet.BlockchainType,
                                    IsPrimary = item.isPrimary,
                                    CreatedBy = item.wallet.CreatorType
                                },
                                BlockchainWalletsBoundedContext.Name,
                                BlockchainWalletsBoundedContext.Name);
                        }

                        await insInMongo;

                        Interlocked.Add(ref counter, queryResult.Wallets.Count());
                        Console.WriteLine($"[{DateTime.UtcNow}] Processed {counter} of unknown");
                    }
                    catch (Exception)
                    {
                        disrupt.Cancel();
                        
                        throw;
                    }
                    finally
                    {
                        throttler.Release();
                    }
                }, disrupt.Token));
                
                continuationToken = queryResult.ContinuationToken;
            } while (continuationToken != null);

            await Task.WhenAll(tasks);
        }
    }
}
