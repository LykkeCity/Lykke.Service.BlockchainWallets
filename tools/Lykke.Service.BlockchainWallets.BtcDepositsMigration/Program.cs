using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Common;
using Lykke.Cqrs;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Service.BlockchainSignFacade.Client;
using Lykke.Service.BlockchainSignFacade.Contract.Models;
using Lykke.Service.BlockchainWallets.AzureRepositories;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Contract.Events;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Core.Settings;
using Lykke.SettingsReader;
using Microsoft.Extensions.CommandLineUtils;
using MoreLinq;
using NBitcoin;
using RestEase;

namespace Lykke.Service.BlockchainWallets.BtcDepositsMigration
{
    internal static class Program
    {
        private const string BwSettingsUrl = "-BWSettingsUrl | -BW";
        private const string BitcoinSettingsUrl = "-BitcoinSettingsUrl | -b";
        private const string SignFacadeApiKey = "-key";

        private const string BlockchainType = "Bitcoin";
        private const string AssetId = "BTC";

        private static void Main(string[] args)
        {
            var application = new CommandLineApplication
            {
                Description = "Migrate all legacy segwit deposit addresses for BTC"
            };

            var options = new Dictionary<string, CommandOption>
            {
                {
                    BwSettingsUrl,
                    application.Option(BwSettingsUrl, "Url of a BlockchainWallets service settings.",
                        CommandOptionType.SingleValue)
                },
                {
                    BitcoinSettingsUrl,
                    application.Option(BitcoinSettingsUrl, "Url of a legacy Bitcoin service settings.",
                        CommandOptionType.SingleValue)
                },
                {
                    SignFacadeApiKey,
                    application.Option(SignFacadeApiKey, "Api key to BlockchainSignFacade with ImportWallet permission",
                        CommandOptionType.SingleValue)
                }
            };
            application.HelpOption("-? | -h | --help");
            application.OnExecute(async () =>
            {
                try
                {
                    if (options.Any(x => !x.Value.HasValue()))
                        application.ShowHelp();
                    else
                        await Migrate
                        (
                            options[BwSettingsUrl].Value(),
                            options[BitcoinSettingsUrl].Value(),
                            options[SignFacadeApiKey].Value()
                        );

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

        private static async Task Migrate(string bwSettingsUrl, string bitcoinSettingsUrl, string apiKey)
        {
            if (!ValidateSettingsUrl(bwSettingsUrl)) return;
            if (!ValidateSettingsUrl(bitcoinSettingsUrl)) return;


            var logFactory = LogFactory.Create().AddConsole();

            var appSettings = new SettingsServiceReloadingManager<AppSettings>(bwSettingsUrl, p=> {});
            var bitcoinSettings = new SettingsServiceReloadingManager<BitcoinAppSettings>(bitcoinSettingsUrl, p => { })
                .CurrentValue.BitcoinService;


            var signingService = RestClient.For<ISigningServiceApi>(new HttpClient { BaseAddress = new Uri(bitcoinSettings.SignatureProviderUrl) });
            signingService.ApiKey = bitcoinSettings.SigningServiceApiKey;

            var walletRepository = BlockchainWalletsRepository.Create(appSettings.Nested(o => o.BlockchainWalletsService.Db.DataConnString), logFactory);

            var firstGenerationBlockchainWalletRepository = FirstGenerationBlockchainWalletRepository.Create(
                appSettings.Nested(o => o.BlockchainWalletsService.Db.ClientPersonalInfoConnString), logFactory);

            var cqrs = Cqrs.CreateCqrsEngine(appSettings.CurrentValue.BlockchainWalletsService.Cqrs.RabbitConnectionString, logFactory);

            var blockchainSignFacade = new BlockchainSignFacadeClient
            (
                appSettings.CurrentValue.BlockchainSignFacadeClient.ServiceUrl,
                apiKey,
                logFactory.CreateLog(nameof(BlockchainSignFacadeClient))
            );

            var counter = 0;
            const int batchSize = 10;

            await firstGenerationBlockchainWalletRepository.EnumerateBcnCredsByChunksAsync(AssetId, async records =>
            {
                foreach (var batch in records.Batch(batchSize))
                {
                    await batch.SelectAsync(async o =>
                    {
                        await Migrate(walletRepository, signingService, blockchainSignFacade, cqrs, o);
                        return true;
                    });

                    counter += batchSize;
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write($"{counter} wallets migrated");
                }
            });
            Console.WriteLine();
            Console.WriteLine("Migration completed");
        }


        private static bool ValidateSettingsUrl(string settingsUrl)
        {
            if (!Uri.TryCreate(settingsUrl, UriKind.Absolute, out _))
            {
                Console.WriteLine($"SettingsUrl should be a valid uri");
                return false;
            }

            return true;
        }

        private static async Task<string> GetPrivateKey(ISigningServiceApi signingServiceApi, string address)
        {
            try
            {
                return (await signingServiceApi.GetPrivateKey(address)).PrivateKey;
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"Private key for address {address} is not found. Error - {e.Message + e.StackTrace}");
                return null;
            }
        }

        private static async Task Migrate(IBlockchainWalletsRepository walletRepository, ISigningServiceApi signingServiceApi,
            IBlockchainSignFacadeClient blockchainSignFacade, ICqrsEngine cqrs,
            IBcnCredentialsRecord bcnCredentialsRecord)
        {
            var clientId = Guid.Parse(bcnCredentialsRecord.ClientId);
            var existingWallet = await walletRepository.TryGetAsync(BlockchainType,  clientId);
            if (existingWallet != null)
                return;
            var address = bcnCredentialsRecord.AssetAddress;
            var privateKey = await GetPrivateKey(signingServiceApi, address);
            if (privateKey == null)
                return;

            await ImportWalletToSignFacade(blockchainSignFacade, privateKey, address);

            await walletRepository.AddAsync(BlockchainType,  clientId, address, CreatorType.LykkeWallet);
            var @event = new WalletCreatedEvent
            {
                Address = address,
                AssetId = AssetId,
                BlockchainType = BlockchainType,
                IntegrationLayerId = BlockchainType,
                ClientId = clientId,
                CreatedBy = CreatorType.LykkeWallet
            };
            cqrs.PublishEvent(@event, BlockchainWalletsBoundedContext.Name);
        }

        private static async Task ImportWalletToSignFacade(IBlockchainSignFacadeClient blockchainSignFacade,
            string privateKey,
            string address)
        {
            try
            {
                await blockchainSignFacade.ImportWalletAsync(BlockchainType, new ImportWalletRequest
                {
                    PrivateKey = privateKey,
                    AddressContext =
                        new AddressContextContract { PubKey = Key.Parse(privateKey).PubKey.ToString() }.ToJson(),
                    PublicAddress = address
                });
            }
            catch (ErrorResponseException e) when (e.StatusCode == HttpStatusCode.Conflict)
            {
                Console.WriteLine($"Private key for address {address} was imported already");
            }
        }

        public class AddressContextContract
        {
            public string PubKey { get; set; }
        }
    }
}
