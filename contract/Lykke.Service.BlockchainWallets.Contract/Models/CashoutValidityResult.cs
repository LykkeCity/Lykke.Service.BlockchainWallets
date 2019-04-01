using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lykke.Service.BlockchainWallets.Contract.Models
{
    [DataContract]
    public class CashoutValidityResult
    {
        [DataMember]
        public IEnumerable<ValidationErrorResponse> ValidationErrors { get; set; }

        [DataMember]
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

    [DataContract]
    public class ValidationErrorResponse
    {
        [DataMember]
        public ValidationErrorType Type { get; private set; }

        [DataMember]
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
