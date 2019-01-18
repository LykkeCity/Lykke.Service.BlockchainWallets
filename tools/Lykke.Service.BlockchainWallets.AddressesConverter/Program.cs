using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Service.BlockchainWallets.AzureRepositories;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.Settings;
using Lykke.SettingsReader;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Lykke.Service.BlockchainWallets.AddressesConverter
{
    internal static class Program
    {
        private const string SettingsUrl = "settingsUrl";
        private const string IntegrationId = "integrationId";
        private const string AssetId = "assetId";

        private static void Main(string[] args)
        {
            var application = new CommandLineApplication
            {
                Description = "Converts all default addresses to additional for specified integration and asset."
            };

            var arguments = new Dictionary<string, CommandArgument>
            {
                { SettingsUrl, application.Argument(SettingsUrl, "Url of a BlockchainWallets service settings.") },
                { IntegrationId, application.Argument(IntegrationId, "Id of a blockchain integration layer.") },
                { AssetId, application.Argument(AssetId, "Id of a blockchain integration layer asset.") }
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
                        await ConvertAddressesAsync
                        (
                            arguments[SettingsUrl].Value,
                            arguments[IntegrationId].Value,
                            arguments[AssetId].Value
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

        private static async Task ConvertAddressesAsync(string settingsUrl, string integrationId, string assetId)
        {
            if (!Uri.TryCreate(settingsUrl, UriKind.Absolute, out _))
            {
                Console.WriteLine($"{SettingsUrl} should be a valid uri");

                return;
            }
            
            var logFactory = LogFactory.Create()
                .AddConsole();

            var settings = new SettingsServiceReloadingManager<AppSettings>(settingsUrl, p => { }).Nested(x => x.BlockchainWalletsService.Db.DataConnString);
            
            var defaultWalletsRepository = (WalletRepository) WalletRepository.Create(settings, logFactory);
            var additionalWalletsRepository = AdditionalWalletRepository.Create(settings, logFactory);

            string continuationToken = null;

            Console.WriteLine("Converting wallets...");
            
            var progressCounter = 0;

            do
            {
                IEnumerable<WalletDto> defaultWallets;

                (defaultWallets, continuationToken) = await defaultWalletsRepository.GetAsync(integrationId, assetId, 100, continuationToken);

                foreach (var defaultWallet in defaultWallets)
                {
                    await additionalWalletsRepository.AddAsync
                    (
                        defaultWallet.BlockchainType,
                        defaultWallet.AssetId,
                        defaultWallet.ClientId,
                        defaultWallet.Address
                    );

                    await defaultWalletsRepository.DeleteIfExistsAsync
                    (
                        defaultWallet.BlockchainType,
                        defaultWallet.AssetId,
                        defaultWallet.ClientId
                    );

                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write($"{++progressCounter} wallets converted");
                }


            } while (continuationToken != null);

            if (progressCounter == 0)
            {
                Console.WriteLine("Nothing to convert");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Conversion completed");
            }
        }
    }
}
