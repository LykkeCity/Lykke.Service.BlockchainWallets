using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Common.Log;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Service.BlockchainWallets.AzureRepositories.Backup;
using Lykke.Service.BlockchainWallets.Core.Settings;
using Lykke.Service.BlockchainWallets.MongoRepositories.Wallets;
using Lykke.SettingsReader;
using Microsoft.Extensions.CommandLineUtils;

namespace Lykke.Service.BlockchainWallets.RestoreBackupToPrimaryStorage
{
    class Program
    {
        private const string BwSettingsUrl = "-BWSettingsUrl | -BW";

        private static void Main(string[] args)
        {
            var application = new CommandLineApplication
            {
                Description = "Resores backup storage to mongo primary"
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
            var logFactory = LogFactory.Create().AddUnbufferedConsole();
            var appSettings = new SettingsServiceReloadingManager<AppSettings>(settingsUrl, p => { });

            var walletMongoRepo = BlockchainWalletMongoRepository.Create(
                appSettings.CurrentValue.BlockchainWalletsService.Db.Mongo.ConnString,
                appSettings.CurrentValue.BlockchainWalletsService.Db.Mongo.DbName,
                logFactory);

            var backupStorage =
                BlockchainWalletsBackupRepository.Create(
                    appSettings.Nested(p => p.BlockchainWalletsService.Db.DataConnString), logFactory);

            var log = logFactory.CreateLog(nameof(Program));

            var take = 50;
            string continuationToken = null;
            var counter = 0;
            do
            {
                var queryResult = await backupStorage.GetDataWithContinuationTokenAsync(take, continuationToken);

                counter += queryResult.Entities.Count();

                log.Info($"Processing {counter} of unknown");

                await walletMongoRepo.InsertBatchAsync(queryResult.Entities.Select(p =>
                    (blockchainType: p.integrationLayerId, clientId: p.clientId,
                        address: p.address,
                        createdBy: p.createdBy, isPrimary: p.isPrimary)));

                continuationToken = queryResult.ContinuationToken;
            } while (continuationToken != null);
        }
    }
}
