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

            const int take = 50;
            string continuationToken = null;

            var counter = 0;
            do
            {
                var queryResult = await obsoleteRepo.GetAllAsync(take, continuationToken);
                await queryResult.Wallets.ForEachAsyncSemaphore(16,async item =>
                {
                    Interlocked.Increment(ref counter);
                    Console.WriteLine($"Processing {item.wallet.ClientId}-{item.wallet.BlockchainType}-{item.wallet.Address} {counter} of unknown");

                    await walletMongoRepo.AddAsync(item.wallet.BlockchainType, item.wallet.ClientId, item.wallet.Address,
                        item.wallet.CreatorType, addAsLatest: item.isPrimary);

                    await backupStorage.AddAsync(item.wallet.BlockchainType, item.wallet.ClientId, item.wallet.Address,
                        item.wallet.CreatorType, item.isPrimary);
                });

                continuationToken = queryResult.ContinuationToken;
            } while (continuationToken != null);
        }
    }
}
