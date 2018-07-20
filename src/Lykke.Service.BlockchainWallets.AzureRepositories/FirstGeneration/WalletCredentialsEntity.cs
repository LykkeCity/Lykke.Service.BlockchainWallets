using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.BlockchainWallets.Core.FirstGeneration;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.BlockchainWallets.AzureRepositories.FirstGeneration
{
    public class WalletCredentialsEntity : FirstGenerationBlockchainWalletEntity.FromWalletCredentials
    {
        public static class ByClientId
        {
            public static string GeneratePartitionKey()
            {
                return "Wallet";
            }

            public static string GenerateRowKey(string clientId)
            {
                return clientId;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.ClientId);
                return entity;
            }
        }

        public static class ByColoredMultisig
        {
            public static string GeneratePartitionKey()
            {
                return "WalletColoredMultisig";
            }

            public static string GenerateRowKey(string coloredMultisig)
            {
                return coloredMultisig;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.ColoredMultiSig);
                return entity;
            }
        }

        public static class ByMultisig
        {
            public static string GeneratePartitionKey()
            {
                return "WalletMultisig";
            }

            public static string GenerateRowKey(string multisig)
            {
                return multisig;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.MultiSig);
                return entity;
            }
        }

        public static class ByEthContract
        {
            public static string GeneratePartitionKey()
            {
                return "EthConversionWallet";
            }

            public static string GenerateRowKey(string ethWallet)
            {
                return ethWallet;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.EthConversionWalletAddress);
                return entity;
            }
        }

        public static class BySolarCoinWallet
        {
            public static string GeneratePartitionKey()
            {
                return "SolarCoinWallet";
            }

            public static string GenerateRowKey(string address)
            {
                return address;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.SolarCoinWalletAddress);
                return entity;
            }
        }

        public static class ByChronoBankContract
        {
            public static string GeneratePartitionKey()
            {
                return "ChronoBankContract";
            }

            public static string GenerateRowKey(string contract)
            {
                return contract;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.ChronoBankContract);
                return entity;
            }
        }

        public static class ByQuantaContract
        {
            public static string GeneratePartitionKey()
            {
                return "QuantaContract";
            }

            public static string GenerateRowKey(string contract)
            {
                return contract;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.QuantaContract);
                return entity;
            }
        }

        public static WalletCredentialsEntity Create(IWalletCredentials src)
        {
            return new WalletCredentialsEntity
            {
                ClientId = src.ClientId,
                PrivateKey = src.PrivateKey,
                Address = src.Address,
                MultiSig = src.MultiSig,
                ColoredMultiSig = src.ColoredMultiSig,
                PreventTxDetection = src.PreventTxDetection,
                EncodedPrivateKey = src.EncodedPrivateKey,
                PublicKey = src.PublicKey,
                BtcConvertionWalletPrivateKey = src.BtcConvertionWalletPrivateKey,
                BtcConvertionWalletAddress = src.BtcConvertionWalletAddress,
                EthConversionWalletAddress = src.EthConversionWalletAddress,
                EthAddress = src.EthAddress,
                EthPublicKey = src.EthPublicKey,
                SolarCoinWalletAddress = src.SolarCoinWalletAddress,
                ChronoBankContract = src.ChronoBankContract,
                QuantaContract = src.QuantaContract
            };
        }

        public static void Update(WalletCredentialsEntity src, IWalletCredentials changed)
        {
            src.ClientId = changed.ClientId;
            src.PrivateKey = changed.PrivateKey;
            src.Address = changed.Address;
            src.MultiSig = changed.MultiSig;
            src.ColoredMultiSig = changed.ColoredMultiSig;
            src.PreventTxDetection = changed.PreventTxDetection;
            src.EncodedPrivateKey = changed.EncodedPrivateKey;
            src.PublicKey = changed.PublicKey;
            src.BtcConvertionWalletPrivateKey = changed.BtcConvertionWalletPrivateKey;
            src.BtcConvertionWalletAddress = changed.BtcConvertionWalletAddress;
            src.EthConversionWalletAddress = changed.EthConversionWalletAddress;
            src.EthAddress = changed.EthAddress;
            src.EthPublicKey = changed.EthPublicKey;
            src.SolarCoinWalletAddress = changed.SolarCoinWalletAddress;
            src.ChronoBankContract = changed.ChronoBankContract;
            src.QuantaContract = changed.QuantaContract;
        }
    }
}
