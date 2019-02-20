using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Service.BlockchainWallets.AzureRepositories.Backup;
using Lykke.Service.BlockchainWallets.Core.Settings;
using Lykke.Service.BlockchainWallets.MongoRepositories.Wallets;
using Lykke.Service.BlockchainWallets.ObsoleteAzureToMongoMigrator.Helpers;
using Lykke.Service.BlockchainWallets.ObsoleteAzureToMongoMigrator.ObsoleteAzurePepo;
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

            var backupStorage =
                BlockchainWalletsBackupRepository.Create(
                    appSettings.Nested(p => p.BlockchainWalletsService.Db.DataConnString), logfactory);
            
            Console.WriteLine("Ensuring indexes created");
            await walletMongoRepo.EnsureIndexesCreatedAsync();

            const int take = 1000;
            string continuationToken = null;

            var counter = 0;

            var throttler = new SemaphoreSlim(8);
            var tasks = new List<Task>();
            do
            {

                await throttler.WaitAsync();
                var queryResult = await obsoleteRepo.GetAllAsync(take, continuationToken);

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        Interlocked.Add(ref counter, queryResult.Wallets.Count());

                        Console.WriteLine($"Processing {counter} of unknown");

                        await walletMongoRepo.InsertBatchAsync(queryResult.Wallets.Select(p =>
                            (blockchainType: p.wallet.BlockchainType, clientId: p.wallet.ClientId, address: p.wallet.Address,
                                createdBy: p.wallet.CreatorType, addAsLatest: p.isPrimary)));
                    }
                    finally
                    {
                        throttler.Release();
                    }
                }));
                
                continuationToken = queryResult.ContinuationToken;
            } while (continuationToken != null);

            await Task.WhenAll(tasks);
        }
    }
}
