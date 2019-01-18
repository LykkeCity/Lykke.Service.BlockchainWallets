using AzureStorage.Tables.Templates.Index;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Service.BlockchainWallets.AzureRepositories;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.DTOs;
using Lykke.Service.BlockchainWallets.Core.Settings;
using Lykke.SettingsReader;
using Microsoft.Extensions.CommandLineUtils;
using MoreLinq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.BlockchainWallets.MigrateAdditionalWalletsIndexes
{
    internal static class Program
    {
        private const string SettingsUrl = "settingsUrl";

        private static void Main(string[] args)
        {
            var application = new CommandLineApplication
            {
                Description = "Migrate additional wallets addresses"
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

            var walletsRepository = (BlockchainWalletsRepository)BlockchainWalletsRepository.Create(settings, logFactory);
            throw new NotImplementedException();
        }
    }

    internal class AddWallet
    {
        public string BlockchainType { get; set; }
        public Guid ClientId { get; set; }
        public string Address { get; set; }
        public CreatorType CreatedBy { get; set; }
        public string ClientLatestDepositIndexManualPartitionKey { get; set; }
        public bool AddAsLatest { get; set; }
    }
}
