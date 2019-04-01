namespace Lykke.Service.BlockchainWallets.Core.DTOs.Validation
{
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
}
