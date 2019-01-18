using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Tables;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Service.BlockchainWallets.AzureRepositories;
using Lykke.Service.BlockchainWallets.BchDepositsMigration.Address;
using Lykke.Service.BlockchainWallets.BchDepositsMigration.ObservableWallet;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;
using Lykke.Service.BlockchainWallets.Core.Settings;
using Lykke.SettingsReader;
using Microsoft.Extensions.CommandLineUtils;
using NBitcoin;
using NBitcoin.Altcoins;

namespace Lykke.Service.BlockchainWallets.BchDepositsMigration
{
    class Program
    {
        private const string BitcoinCashDataConnString = "bitcoin cash data connection string";
        private const string BitcoinCashNetwork = "Bitcoin cash network";
        private const string SettingsUrl = "settingsUrl";
        private const string BlockchainType = "blockchainType";

        static void Main(string[] args)
        {
            var application = new CommandLineApplication
            {
                Description = "Convert bitcoin cash addresses to actual format"
            };

            var arguments = new Dictionary<string, CommandArgument>
            {
                { BitcoinCashDataConnString, application.Argument(BitcoinCashDataConnString, "Bitcoin cash azure storage") },
                { BitcoinCashNetwork, application.Argument(BitcoinCashNetwork, "Bitcoin cash network mainnet/test") },
                { SettingsUrl, application.Argument(SettingsUrl, "BlockchainWallets settings url") },
                { BlockchainType, application.Argument(BlockchainType, "Blockchain type (for bitcoin cash abc/bitcoin cash sv)") },
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
                        await Execute(arguments[BitcoinCashDataConnString].Value,
                            arguments[SettingsUrl].Value,
                            arguments[BitcoinCashNetwork].Value,
                            arguments[BlockchainType].Value);
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
        private static async Task Execute(string bitcoinCashDataConnString, 
            string settingsUrl,
            string bitcoinCashNetwork,
            string blockchainType)
        {
            BCash.Instance.EnsureRegistered();
            var network = Network.GetNetwork(bitcoinCashNetwork);
            var bcashNetwork = network == Network.Main ? BCash.Instance.Mainnet : BCash.Instance.Regtest;

            var logConsole = LogFactory.Create().AddConsole();
            var settings = new SettingsServiceReloadingManager<AppSettings>(settingsUrl, p=>{});

            var walletRepo = BlockchainWalletsRepository.Create(settings.Nested(p => p.BlockchainWalletsService.Db.DataConnString),
                logConsole);

            var firstGenerationBlockchainWalletRepository = FirstGenerationBlockchainWalletRepository.Create(settings.Nested(x => x.BlockchainWalletsService.Db.ClientPersonalInfoConnString), logConsole);
            
            var observableWalletsRepo = ObservableWalletRepository.Create(new ReloadingManagerAdapter<string>(bitcoinCashDataConnString),
                logConsole);

            var addressValidator = new AddressValidator(network, bcashNetwork);

            Console.WriteLine("Retrieving observable wallets");

            var observableWallets = (await observableWalletsRepo.GetAll()).ToList();

            Console.WriteLine("Processing items");

            var counter = 0;
            foreach (var observableWallet in observableWallets)
            {
                counter++;
                Console.WriteLine($"Processing {observableWallet.Address} -- {counter} of {observableWallets.Count}");

                var address = addressValidator.GetBitcoinAddress(observableWallet.Address);
                
                if (address == null)
                {
                    throw new ArgumentException($"Unrecognized address {observableWallet.Address}",
                        nameof(observableWallet.Address));
                }

                var wallet = await walletRepo.TryGetAsync(blockchainType, observableWallet.Address);

                if (wallet == null)
                {
                    var prevColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Wallet not found {observableWallet.Address} -{blockchainType}");
                    Console.ForegroundColor = prevColor;

                    continue;
                }

                var replacedAddr = address.ScriptPubKey.GetDestinationAddress(bcashNetwork).ToString();
                await walletRepo.AddAsync(wallet.BlockchainType, wallet.ClientId, replacedAddr, wallet.CreatorType);

                var oldCredsRecord = new BcnCredentialsRecord
                {
                    Address = string.Empty,
                    AssetAddress = replacedAddr,
                    ClientId = wallet.ClientId.ToString(),
                    EncodedKey = string.Empty,
                    PublicKey = string.Empty,
                    AssetId = $"{blockchainType} ({wallet.AssetId})"
                };

                await firstGenerationBlockchainWalletRepository.DeleteIfExistAsync(oldCredsRecord);

                var newCredsRecord = new BcnCredentialsRecord
                {
                    Address = string.Empty,
                    AssetAddress = replacedAddr,
                    ClientId = wallet.ClientId.ToString(),
                    EncodedKey = string.Empty,
                    PublicKey = string.Empty,
                    AssetId = $"{blockchainType} ({wallet.AssetId})"
                };

                await firstGenerationBlockchainWalletRepository.InsertOrReplaceAsync(newCredsRecord);

                await walletRepo.DeleteIfExistsAsync(wallet.BlockchainType, wallet.ClientId, wallet.Address);
            }

            Console.WriteLine("All done");
        }
    }
}
