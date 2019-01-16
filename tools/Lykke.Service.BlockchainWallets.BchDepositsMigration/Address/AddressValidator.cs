using System;
using NBitcoin;
using NBitcoin.Altcoins;

namespace Lykke.Service.BlockchainWallets.BchDepositsMigration.Address
{
    public class AddressValidator 
    {
        private readonly Network _network;
        private readonly Network _bcashNetwork;

        public AddressValidator(Network network, Network bcashNetwork)
        {
            _network = network;
            _bcashNetwork = bcashNetwork;
        }


        public bool IsValid(string address)
        {
            var addr = GetBitcoinAddress(address);

            return addr != null;
        }


        private BitcoinAddress GetBitcoinAddress(string address, Network network)
        {
            try
            {
                return BitcoinAddress.Create(address, network);
            }
            catch (Exception)
            {
                try
                {
                    return new BitcoinColoredAddress(address, network).Address;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }


        public BitcoinAddress GetBitcoinAddress(string address)
        {
            //eg moc231tgxApbRSwLNrc9ZbSVDktTRo3acK
            var legacyAddress = GetBitcoinAddress(address, _network);
            if (legacyAddress != null)
                return legacyAddress;

            //eg: bitcoincash:qpm2qsznhks23z7629mms6s4cwef74vcwvy22gdx6a
            var canonicalAddress = GetBitcoinAddress(address, _bcashNetwork);

            if (canonicalAddress != null)
                return canonicalAddress;

            //eg qpm2qsznhks23z7629mms6s4cwef74vcwvy22gdx6a
            var addressWithoutPrefix =
                GetBitcoinAddress($"{GetAddressPrefix(_bcashNetwork)}:{address?.Trim()}", _bcashNetwork);

            return addressWithoutPrefix;
        }

        private static string GetAddressPrefix(Network bcashNetwork)
        {
            if (bcashNetwork == BCash.Instance.Mainnet)
            {
                return "bitcoincash";
            }
            if (bcashNetwork == BCash.Instance.Regtest)
            {
                return "bchreg";
            }
            if (bcashNetwork == BCash.Instance.Testnet)
            {
                return "bchtest";
            }

            throw new ArgumentException("Unknown bcash network", nameof(bcashNetwork));
        }
    }
}
