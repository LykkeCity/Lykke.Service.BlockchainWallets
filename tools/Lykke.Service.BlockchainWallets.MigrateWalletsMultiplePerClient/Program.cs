using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Service.BlockchainWallets.Core.Settings;
using Lykke.SettingsReader;
using Microsoft.Extensions.CommandLineUtils;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.Service.BlockchainWallets.AzureRepositories;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.DTOs;

namespace Lykke.Service.BlockchainWallets.MigrateWalletsMultiplePerClient
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

            var archiveWalletsTable = AzureTableStorage<BlockchainWalletEntity>.Create
            (
                settings,
                "BlockchainWalletsArchive",
                logFactory
            );

            var defaultWalletsRepository = (WalletRepository)WalletRepository.Create(settings, logFactory);
            var blockchainWalletsRepository = (BlockchainWalletsRepository)AzureRepositories.BlockchainWalletsRepository.Create(settings, logFactory);

            string continuationToken = null;

            Console.WriteLine("Creating indexes for default wallets...");

            var progressCounter = 0;
            const int batchSize = 10;

            do
            {
                try
                {
                    IEnumerable<WalletDto> wallets;
                    (wallets, continuationToken) = await blockchainWalletsRepository.GetAllAsync(100, continuationToken);

                    foreach (var batch in wallets.Batch(batchSize))
                    {
                        await Task.WhenAll(batch.Select(o =>
                            blockchainWalletsRepository.DeleteIfExistsAsync(o.BlockchainType, o.ClientId, o.Address)));
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

            do
            {
                try
                {
                    IEnumerable<BlockchainWalletEntity> wallets;
                    (wallets, continuationToken) = await archiveWalletsTable.GetDataWithContinuationTokenAsync(100, continuationToken);

                    foreach (var batch in wallets.Batch(batchSize))
                    {
                        await Task.WhenAll(batch.Select(o =>
                            blockchainWalletsRepository.AddAsync(o.IntegrationLayerId, o.ClientId, o.Address, o.CreatedBy)));
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

            do
            {
                try
                {
                    IEnumerable<WalletDto> wallets;
                    (wallets, continuationToken) = await defaultWalletsRepository.GetAllAsync(100, continuationToken);

                    foreach (var batch in wallets.Batch(batchSize))
                    {
                        await Task.WhenAll(batch.Select(o =>
                            blockchainWalletsRepository.AddAsync(o.BlockchainType, o.ClientId, o.Address, CreatorType.LykkeWallet)));
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
