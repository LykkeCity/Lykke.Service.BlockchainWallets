using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Service.BlockchainWallets.Core.Settings;
using Lykke.Service.BlockchainWallets.MigrateWalletsIndexes.Lykke.Service.BlockchainWallets.AzureRepositories;
using Lykke.SettingsReader;
using Microsoft.Extensions.CommandLineUtils;
using MoreLinq;

namespace Lykke.Service.BlockchainWallets.MigrateWalletsIndexes
{
    internal static class Program
    {
        private const string SettingsUrl = "settingsUrl";

        private static void Main(string[] args)
        {
            var application = new CommandLineApplication
            {
                Description = "Migrate wallets address indexes"
            };

            var arguments = new Dictionary<string, CommandArgument>
            {
                { SettingsUrl, application.Argument(SettingsUrl, "Url of a BlockchainWallets service settings.") },
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
                        await Migrate
                        (
                            arguments[SettingsUrl].Value
                        );
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

        private static async Task Migrate(string settingsUrl)
        {
            if (!Uri.TryCreate(settingsUrl, UriKind.Absolute, out _))
            {
                Console.WriteLine($"{SettingsUrl} should be a valid uri");

                return;
            }

            var logFactory = LogFactory.Create().AddConsole();

            var settings = new SettingsServiceReloadingManager<AppSettings>(settingsUrl).Nested(x => x.BlockchainWalletsService.Db.DataConnString);

            var defaultWalletsRepository = WalletRepository.Create(settings, logFactory);
            var additionalWalletsRepository = AdditionalWalletRepository.Create(settings, logFactory);

            string continuationToken = null;

            Console.WriteLine("Drop default address indexes");

            await defaultWalletsRepository.DeleteAllAddressIndexesAsync();

            Console.WriteLine("Creating indexes for default wallets...");

            var progressCounter = 0;
            const int batchSize = 10;
            do
            {
                try
                {
                    IEnumerable<WalletEntity> wallets;
                    (wallets, continuationToken) = await defaultWalletsRepository.GetAsync(100, continuationToken);

                    foreach (var batch in wallets.Batch(batchSize))
                    {
                        await Task.WhenAll(batch.Select(o => defaultWalletsRepository.AddAddressIndex(o)));
                        progressCounter += batchSize;
                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.Write($"{progressCounter} indexes created");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace + " " + e.Message);
                }

            } while (continuationToken != null);
            Console.WriteLine();
            Console.WriteLine("Drop additional address indexes");

            await additionalWalletsRepository.DeleteAllAddressIndexesAsync();

            Console.WriteLine("Creating indexes for additional wallets...");

            progressCounter = 0;
            do
            {
                try
                {
                    IEnumerable<AdditionalWalletEntity> wallets;
                    (wallets, continuationToken) = await additionalWalletsRepository.GetAsync(100, continuationToken);
                    foreach (var batch in wallets.Batch(batchSize))
                    {
                        await Task.WhenAll(batch.Select(o => additionalWalletsRepository.AddAddressIndex(o)));

                        progressCounter += batchSize;
                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.Write($"{progressCounter} indexes created");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace + " " + e.Message);
                }

            } while (continuationToken != null);

            Console.WriteLine();
            Console.WriteLine("Conversion completed");
        }
    }
}
