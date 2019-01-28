using Autofac;
using Lykke.Service.Assets.Client;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Core.Settings;
using Lykke.Service.BlockchainWallets.Modules;
using Lykke.SettingsReader;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Common.Log;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Service.Assets.Client.Models;

namespace Lykke.Service.BlockchainWallets.TestLegacy
{
    internal static class Program
    {
        private const string SettingsUrl = "settingsUrl";
        private const string ClientId = "clientId";

        private static void Main(string[] args)
        {
            var application = new CommandLineApplication
            {
                Description = "Converts all default addresses to additional for specified integration and asset."
            };

            var arguments = new Dictionary<string, CommandArgument>
            {
                {SettingsUrl, application.Argument(SettingsUrl, "Url of a BlockchainWallets service settings.")},
                {ClientId, application.Argument(ClientId, "Id of a user to retrieve all legacy deposits.")}
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
                        await CreateAllLegacyAssetsForUser
                        (
                            arguments[SettingsUrl].Value,
                            arguments[ClientId].Value
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

        private static async Task CreateAllLegacyAssetsForUser(string settingsUrl, string clientId)
        {
            if (!Uri.TryCreate(settingsUrl, UriKind.Absolute, out _))
            {
                Console.WriteLine($"{SettingsUrl} should be a valid uri");

                return;
            }

            var logFactory = LogFactory.Create()
                .AddConsole();

            var appSettings = new SettingsServiceReloadingManager<AppSettings>(settingsUrl, p => { });

            var builder = new ContainerBuilder();

            builder.RegisterInstance(logFactory)
                .As<ILogFactory>()
                .SingleInstance();

            builder
                .RegisterModule(new CqrsModule(appSettings.CurrentValue.BlockchainWalletsService.Cqrs))
                .RegisterModule(new RepositoriesModule(appSettings.Nested(x => x.BlockchainWalletsService.Db)))
                .RegisterModule(new ServiceModule(
                    appSettings.CurrentValue.BlockchainsIntegration,
                    appSettings.CurrentValue.BlockchainSignFacadeClient,
                    appSettings.CurrentValue,
                    appSettings.CurrentValue.AssetsServiceClient,
                    appSettings.CurrentValue.BlockchainWalletsService));

            var resolver = builder.Build();

            var assetsServiceWithCache = resolver.Resolve<IAssetsServiceWithCache>();
            var walletService = resolver.Resolve<IWalletService>();

            var allAssets = await assetsServiceWithCache.GetAllAssetsAsync(false);
            var coloredAsset = allAssets.FirstOrDefault(x => x.Blockchain == Blockchain.Bitcoin
                                                             && !string.IsNullOrEmpty(x.BlockChainAssetId));

            //DEV assets
            var assetListToTest = new List<string>()
            {
                SpecialAssetIds.BitcoinAssetId,
                SpecialAssetIds.SolarAssetId,
                "a61a3cad-bd63-422a-82a6-3464234856b0", //erc20
                coloredAsset?.Id //ColoredCoin

            };

            var depositsList = new List<string>();

            foreach (var id in assetListToTest)
            {
                //var deposit = await walletService.CreateWalletAsync(SpecialBlockchainTypes.FirstGenerationBlockchain,
                //    id, 
                //    Guid.Parse(clientId));
                //
                //depositsList.Add(deposit.BaseAddress);

                var result = await walletService.TryGetFirstGenerationBlockchainAddressAsync(id,
                    Guid.Parse(clientId));
            }
        }
    }
}
