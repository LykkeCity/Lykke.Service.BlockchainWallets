using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace Lykke.Service.BlockchainWallets.Contract.Models
{
    public class CashoutValidityResult
    {
        public IEnumerable<ValidationErrorResponse> ValidationErrors { get; set; }

        public bool IsAllowed { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ValidationErrorType
    {
        None,
        AddressIsNotValid,
        FieldIsNotValid,
        LessThanMinCashout,
        HotwalletTargetProhibited,
        BlackListedAddress,
        DepositAddressNotFound,
        CashoutToSelfAddress,
        Error
    }

    public class ValidationErrorResponse
    {
        public ValidationErrorType Type { get; private set; }

        public string Value { get; private set; }

        public static ValidationErrorResponse Create(ValidationErrorType type, string value)
        {
            return new ValidationErrorResponse
            {
                Type = type,
                Value = value
            };
        }
    }
}
