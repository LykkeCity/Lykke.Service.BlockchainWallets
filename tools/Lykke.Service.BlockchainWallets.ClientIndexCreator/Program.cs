using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.BlockchainWallets.AzureRepositories;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.Settings;
using Lykke.SettingsReader;
using Microsoft.Extensions.CommandLineUtils;

namespace Lykke.Service.BlockchainWallets.ClientIndexCreator
{
    internal static class Program
    {
        private const string SettingsUrl = "settingsUrl";

        private static void Main(string[] args)
        {
            var application = new CommandLineApplication
            {
                Description = "Creates client indexes for all wallets."
            };

            var arguments = new Dictionary<string, CommandArgument>
            {
                { SettingsUrl, application.Argument(SettingsUrl, "Url of a BlockchainWallets service settings.") }
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
                        await CreateIndexesAsync
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

        private static async Task CreateIndexesAsync(string settingsUrl)
        {
            if (!Uri.TryCreate(settingsUrl, UriKind.Absolute, out _))
            {
                Console.WriteLine($"{SettingsUrl} should be a valid uri");

                return;
            }

            var log = new LogToConsole();
            var settings = new SettingsServiceReloadingManager<AppSettings>(settingsUrl).Nested(x => x.BlockchainWalletsService.Db.DataConnString);
            
            var defaultWalletsRepository = (WalletRepository) WalletRepository.Create(settings, log);

            string continuationToken = null;

            Console.WriteLine("Creating Indexes...");
            
            var progressCounter = 0;

            do
            {
                try
                {
                    IEnumerable<WalletDto> wallets;

                    (wallets, continuationToken) = await defaultWalletsRepository.GetAllAsync(100, continuationToken);

                    foreach (var defaultWallet in wallets)
                    {
                        await defaultWalletsRepository.AddAsync
                        (
                            defaultWallet.BlockchainType,
                            defaultWallet.AssetId,
                            defaultWallet.ClientId,
                            defaultWallet.Address
                        );

                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.Write($"{++progressCounter} indexes created");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace + " " + e.Message);
                }

            } while (continuationToken != null);

            if (progressCounter == 0)
            {
                Console.WriteLine("Nothing to create");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine($"Added indexes to {progressCounter} wallets");
            }
        }
    }
}
