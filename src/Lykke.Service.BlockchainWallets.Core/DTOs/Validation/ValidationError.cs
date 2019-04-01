namespace Lykke.Service.BlockchainWallets.Core.DTOs.Validation
{
    public class ValidationError
    {
        public ValidationErrorType Type { get; private set; }
        public string Value { get; private set; }

        public static ValidationError Create(ValidationErrorType type, string value)
        {
            return new ValidationError
            {
                Type = type,
                Value = value
            };
        }

        public static ValidationError CreateError(string value)
        {
            return new ValidationError
            {
                Type = ValidationErrorType.Error,
                Value = value
            };
        }
    }
}
