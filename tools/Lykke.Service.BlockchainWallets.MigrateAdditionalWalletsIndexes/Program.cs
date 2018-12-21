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
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Service.BlockchainWallets.Contract.Events;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Modules;

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
                {SettingsUrl, application.Argument(SettingsUrl, "Url of a BlockchainWallets service settings.")},
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
            var appSettings = new SettingsServiceReloadingManager<AppSettings>(settingsUrl);

            var builder = new ContainerBuilder();

            builder.RegisterInstance(logFactory).As<ILogFactory>().SingleInstance();
            builder
                .RegisterModule(new CqrsModule(appSettings.CurrentValue.BlockchainWalletsService.Cqrs))
                .RegisterModule(new CustomRepositoryModel(appSettings.Nested(x => x.BlockchainWalletsService.Db)))
                .RegisterModule(new ServiceModule(
                    appSettings.CurrentValue.BlockchainsIntegration,
                    appSettings.CurrentValue.BlockchainSignFacadeClient,
                    appSettings.CurrentValue,
                    appSettings.CurrentValue.AssetsServiceClient));

            var applicationContainer = builder.Build();

            var cqrs = applicationContainer.Resolve<ICqrsEngine>();
            var startApplication = applicationContainer.Resolve<IStartupManager>();
            var walletsRepository = (BlockchainWalletsRepository)applicationContainer.Resolve<IBlockchainWalletsRepository>();
            var additionalWalletsRepository = applicationContainer.Resolve<IAdditionalWalletRepository>();
            startApplication.Start();
            string continuationToken = null;

            var progressCounter = 0;

            progressCounter = 0;
            List<WalletDto> additionalWallets = new List<WalletDto>();
            Console.WriteLine("Uploading additional wallets in memory:");

            do
            {
                try
                {
                    IEnumerable<WalletDto> wallets;
                    (wallets, continuationToken) = await additionalWalletsRepository.GetAsync(100, continuationToken);
                    if (wallets != null && wallets.Any())
                        additionalWallets.AddRange(wallets);

                    progressCounter += wallets?.Count() ?? 0;
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write($"{progressCounter} uploaded");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace + " " + e.Message);
                }
            } while (continuationToken != null);

            Console.WriteLine("Wallets has been uploaded!");

            Console.WriteLine("Grouping by clients!");
            //Already presented additional wallets sorted out by user
            var groupedWallets = additionalWallets
                .GroupBy(x => x.ClientId)
                .ToDictionary(y => y.Key,
                    y => y.GroupBy(z => z.BlockchainType)
                        .ToLookup(v => v.Key, v => v.Select(x => x)));
            var clientIds = groupedWallets.Select(x => x.Key);
            List<AddWallet> simpleInserts =
                new List<AddWallet>(additionalWallets.Count);

            Console.WriteLine("Decide how to insert wallets!");

            foreach (var clientId in clientIds)
            {
                var nestedLookup = groupedWallets[clientId];
                var blockchainTypes = nestedLookup.Select(x => x.Key);
                foreach (var type in blockchainTypes)
                {
                    var dtos = nestedLookup[type];
                    var currentWallet = await walletsRepository.TryGetAsync(type, clientId);
                    if (currentWallet == null)
                    {
                        var list = dtos.SelectMany(x => x).Select((y) =>
                        {
                            return new AddWallet()
                            {
                                BlockchainType = y.BlockchainType,
                                ClientId = y.ClientId,
                                Address = y.Address,
                                CreatedBy = y.CreatorType,
                                ClientLatestDepositIndexManualPartitionKey = null,
                                AddAsLatest = true
                            };
                        });

                        simpleInserts.AddRange(list);
                        continue;
                    }

                    string cToken = null;
                    AzureIndex latestDto = null;
                    do
                    {
                        var (wallets, token) =
                            await walletsRepository.GetClientBlockchainTypeIndices(type, clientId, 100, cToken);
                        cToken = token;
                        latestDto = wallets?.Last();
                    } while (!string.IsNullOrEmpty(cToken));


                    long? index = latestDto != null ? (long?) long.Parse(latestDto.RowKey) : null;
                    {
                        var list = dtos.SelectMany(x => x).Select((y, ind) =>
                        {
                            var incrementedIndex = index.HasValue ? index + ind + 1 : null;

                            return new AddWallet()
                            {
                                BlockchainType = y.BlockchainType,
                                ClientId = y.ClientId,
                                Address = y.Address,
                                CreatedBy = y.CreatorType,
                                ClientLatestDepositIndexManualPartitionKey = incrementedIndex != null
                                    ? string.Format("{0:D19}", incrementedIndex)
                                    : null
                            };
                        });

                        simpleInserts.AddRange(list);
                    }
                }
            }

            Console.WriteLine("Checking existing values!");
            var alreadyExisting = new ConcurrentDictionary<string, AddWallet>();
            var tasks = simpleInserts.Select((item) => Task.Run(async () =>
            {
                try
                {
                    var result = await walletsRepository.TryGetAsync(item.BlockchainType, item.Address);
                    if (result != null)
                    {
                        alreadyExisting[item.Address] = item;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }));

            Task.WaitAll(tasks.ToArray());

            Console.WriteLine("Inserting with back date:");
            progressCounter = 0;
            List<string> issuesThatOccured = new List<string>(20);
            foreach (var dto in simpleInserts)
            {
                if (alreadyExisting.TryGetValue(dto.Address, out var _x))
                    continue;

                try
                {
                    await walletsRepository.AddAsync(dto.BlockchainType,
                        dto.ClientId,
                        dto.Address,
                        dto.CreatedBy == 0 ? CreatorType.LykkeWallet : dto.CreatedBy,
                        dto.ClientLatestDepositIndexManualPartitionKey,
                        dto.AddAsLatest);
                    var @event = new WalletCreatedEvent
                    {
                        Address = dto.Address,
                        BlockchainType = dto.BlockchainType,
                        IntegrationLayerId = dto.BlockchainType,
                        CreatedBy = dto.CreatedBy == 0 ? CreatorType.LykkeWallet : dto.CreatedBy,
                        ClientId = dto.ClientId
                    };

                    cqrs.PublishEvent
                    (
                        @event,
                        BlockchainWalletsBoundedContext.Name
                    );
                }
                catch (Exception e)
                {
                    issuesThatOccured.Add($"Could not add {dto.BlockchainType} {dto.ClientId} {dto.Address}");
                }

                progressCounter += 1;
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"{progressCounter} wallets created");
            }

            Console.WriteLine("Additional wallets transfer has been completed!");

            issuesThatOccured.ForEach(x => Console.WriteLine(x));

            Console.WriteLine("Press any key to close it.");
            Console.ReadKey();
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
