using Lykke.Service.BlockchainWallets.Core.Settings;
using Lykke.Service.BlockchainWallets.MongoRepositories.Wallets;
using Lykke.SettingsReader;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;

namespace Lykke.Service.BlockchainWallets.MongoIndexesBuilder
{
    class Program
    {
        private const string BwSettingsUrl = "-BWSettingsUrl | -BW";

        private static void Main(string[] args)
        {
            var application = new CommandLineApplication
            {
                Description = "Creates indexes for mongodb"
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

            var walletMongoRepo = BlockchainWalletMongoRepository.Create(
                appSettings.CurrentValue.BlockchainWalletsService.Db.Mongo.ConnString,
                appSettings.CurrentValue.BlockchainWalletsService.Db.Mongo.DbName,
                LogFactory.Create().AddUnbufferedConsole());

            await walletMongoRepo.EnsureIndexesCreatedAsync();
        }
    }
}
