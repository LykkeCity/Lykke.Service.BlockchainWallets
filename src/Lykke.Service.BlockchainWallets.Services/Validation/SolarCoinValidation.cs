using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Lykke.Service.BlockchainWallets.Services.Validation
{
    public class SolarCoinValidation
    {
        private static readonly Regex Base58Regex = new Regex(@"^[123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz]+$");

        public static bool ValidateAddress(String walletAddress)
        {
            try
            {
                if (IsValid(walletAddress))
                {
                    Base58Encoding.DecodeWithCheckSum(walletAddress);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }


        private static bool IsValid(string address)
        {
            return !string.IsNullOrEmpty(address) && address[0] == '8' && address.Length < 40 && Base58Regex.IsMatch(address);
        }
    }
}
