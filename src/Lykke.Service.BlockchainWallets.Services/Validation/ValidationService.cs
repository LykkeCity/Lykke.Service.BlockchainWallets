using Common;
using JetBrains.Annotations;
using Lykke.Service.Assets.Client;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Core.DTOs.Validation;
using Lykke.Service.BlockchainWallets.Core.Services;
using Lykke.Service.BlockchainWallets.Core.Services.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.BlockchainWallets.Core;
using Lykke.Service.BlockchainWallets.Core.Exceptions;

namespace Lykke.Service.BlockchainWallets.Services.Validation
{
    [UsedImplicitly]
    public class ValidationService : IValidationService
    {
        private readonly IBlockchainIntegrationService _blockchainIntegrationService;
        private readonly IAssetsServiceWithCache _assetsService;
        private readonly IBlackListService _blackListService;
        private readonly IBlockchainExtensionsService _blockchainExtensionsService;
        private readonly IAddressParser _addressParser;
        private readonly IWalletService _walletService;

        public ValidationService(IBlockchainIntegrationService blockchainIntegrationService,
            IAssetsServiceWithCache assetsService,
            IBlackListService blackListService,
            IBlockchainExtensionsService blockchainExtensionsService,
            IAddressParser addressParser,
            IWalletService walletService)
        {
            _blockchainIntegrationService = blockchainIntegrationService;
            _assetsService = assetsService;
            _blackListService = blackListService;
            _blockchainExtensionsService = blockchainExtensionsService;
            _addressParser = addressParser;
            _walletService = walletService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cashoutModel"></param>
        /// <returns>
        /// ValidationError - client error
        /// ArgumentValidationException - developer error
        /// </returns>
        public async Task<IReadOnlyCollection<ValidationError>> ValidateAsync(CashoutModel cashoutModel)
        {
            var errors = new List<ValidationError>(1);

            if (cashoutModel == null)
                return FieldNotValidResult("cashoutModel can't be null");

            if (string.IsNullOrEmpty(cashoutModel.AssetId))
                return FieldNotValidResult("cashoutModel.AssetId can't be null or empty");

            Asset asset;

            try
            {
                asset = await _assetsService.TryGetAssetAsync(cashoutModel.AssetId);
            }
            catch (Exception)
            {
                throw new OperationException($"Asset with Id-{cashoutModel.AssetId} does not exists", OperationErrorCode.None);
            }

            if (asset == null)
                throw new OperationException($"Asset with Id-{cashoutModel.AssetId} does not exists", OperationErrorCode.None);

            if (asset.IsDisabled)
            {
                errors.Add(ValidationError.Create(ValidationErrorType.None, $"Asset {asset.Id} is disabled"));
            }

            var isAddressValid = true;
            IBlockchainApiClient blockchainClient = null;

            if (asset.Id != LykkeConstants.SolarAssetId)
            {
                if (string.IsNullOrEmpty(asset.BlockchainIntegrationLayerId))
                    throw new OperationException(
                        $"Given asset Id-{cashoutModel.AssetId} is not a part of Blockchain Integration Layer", OperationErrorCode.None);

                blockchainClient = _blockchainIntegrationService.GetApiClient(asset.BlockchainIntegrationLayerId);
            }

            if (string.IsNullOrEmpty(cashoutModel.DestinationAddress)
                || !cashoutModel.DestinationAddress.IsValidPartitionOrRowKey()
                || asset.Id != LykkeConstants.SolarAssetId && blockchainClient != null &&
                !await blockchainClient.IsAddressValidAsync(cashoutModel.DestinationAddress)
                || asset.Id == LykkeConstants.SolarAssetId &&
                !SolarCoinValidation.ValidateAddress(cashoutModel.DestinationAddress)
            )
            {
                isAddressValid = false;
                errors.Add(ValidationError.Create(ValidationErrorType.AddressIsNotValid, "Address is not valid"));
            }

            if (isAddressValid)
            {
                if (asset.Id != LykkeConstants.SolarAssetId)
                {
                    var isBlocked = await _blackListService.IsBlockedWithoutAddressValidationAsync
                    (
                        asset.BlockchainIntegrationLayerId,
                        cashoutModel.DestinationAddress
                    );

                    if (isBlocked)
                    {
                        errors.Add(ValidationError.Create(ValidationErrorType.BlackListedAddress, "Address is in the black list"));
                    }
                }

                if (cashoutModel.Amount.HasValue && Math.Abs(cashoutModel.Amount.Value) < (decimal)asset.CashoutMinimalAmount)
                {
                    var minimalAmount = asset.CashoutMinimalAmount.GetFixedAsString(asset.Accuracy).TrimEnd('0');

                    errors.Add(ValidationError.Create(ValidationErrorType.LessThanMinCashout,
                        $"Please enter an amount greater than {minimalAmount}"));
                }

                if (asset.Id != LykkeConstants.SolarAssetId)
                {
                    var blockchainSettings = _blockchainIntegrationService.GetSettings(asset.BlockchainIntegrationLayerId);

                    if (cashoutModel.DestinationAddress == blockchainSettings.HotWalletAddress)
                    {
                        errors.Add(ValidationError.Create(ValidationErrorType.HotwalletTargetProhibited,
                            "Hot wallet as destitnation address prohibited"));
                    }

                    var isPublicExtensionRequired =
                        _blockchainExtensionsService.IsPublicAddressExtensionRequired(asset.BlockchainIntegrationLayerId);
                    if (isPublicExtensionRequired.HasValue &&
                        isPublicExtensionRequired.Value)
                    {
                        var hotWalletParseResult = _addressParser.ExtractAddressParts(
                            asset.BlockchainIntegrationLayerId,
                            blockchainSettings.HotWalletAddress);

                        var destAddressParseResult = _addressParser.ExtractAddressParts(
                            asset.BlockchainIntegrationLayerId,
                            cashoutModel.DestinationAddress);

                        if (hotWalletParseResult.BaseAddress == destAddressParseResult.BaseAddress)
                        {
                            var existedClientIdAsDestination = await _walletService.TryGetClientIdAsync(
                                asset.BlockchainIntegrationLayerId,
                                cashoutModel.DestinationAddress);

                            if (existedClientIdAsDestination == null)
                            {
                                errors.Add(ValidationError.Create(ValidationErrorType.DepositAddressNotFound,
                                    $"Deposit address {cashoutModel.DestinationAddress} not found"));
                            }
                        }

                        var forbiddenCharacterErrors = await ValidateForForbiddenCharsAsync(
                            destAddressParseResult.BaseAddress,
                            destAddressParseResult.AddressExtension,
                            asset.BlockchainIntegrationLayerId);

                        if (forbiddenCharacterErrors != null)
                        {
                            errors.AddRange(forbiddenCharacterErrors);
                        }

                        if (!string.IsNullOrEmpty(destAddressParseResult.BaseAddress))
                        {
                            if (!cashoutModel.DestinationAddress.Contains(destAddressParseResult.BaseAddress))
                            {
                                errors.Add(ValidationError.Create(ValidationErrorType.FieldIsNotValid,
                                    "Base Address should be part of destination address"));
                            }

                            // full address is already checked by integration,
                            // we don't need to validate it again, 
                            // just ensure that base address is not black-listed
                            var isBlockedBase = await _blackListService.IsBlockedWithoutAddressValidationAsync(
                                asset.BlockchainIntegrationLayerId,
                                destAddressParseResult.BaseAddress);

                            if (isBlockedBase)
                                errors.Add(ValidationError.Create(ValidationErrorType.BlackListedAddress,
                                    "Base Address is in the black list"));
                        }
                    }
                }

                if (cashoutModel.ClientId.HasValue)
                {
                    var destinationClientId = await _walletService.TryGetClientIdAsync
                    (
                        asset.BlockchainIntegrationLayerId,
                        cashoutModel.DestinationAddress
                    );

                    if (destinationClientId.HasValue && destinationClientId == cashoutModel.ClientId.Value)
                    {
                        var error = ValidationError.Create
                        (
                            ValidationErrorType.CashoutToSelfAddress,
                            "Withdrawals to the deposit wallet owned by the customer himself prohibited"
                        );

                        errors.Add(error);
                    }
                }
            }

            return errors;
        }

        private static IReadOnlyCollection<ValidationError> FieldNotValidResult(string message)
        {
            return new[] { ValidationError.Create(ValidationErrorType.FieldIsNotValid, message) };
        }

        private async Task<IEnumerable<ValidationError>> ValidateForForbiddenCharsAsync
            (string baseAddress, string addressExtension, string blockchainType)
        {
            var errors = new List<ValidationError>(1);

            var (isAddressExtensionSupported,
                prohibitedCharsBase,
                prohibitedCharsExtension) = await IsAddressExtensionSupported(blockchainType);

            var baseAddressContainsProhibitedChars = baseAddress.IndexOfAny(prohibitedCharsBase?.ToArray()) != -1;
            if (baseAddressContainsProhibitedChars)
            {
                errors.Add(ValidationError.Create(ValidationErrorType.AddressIsNotValid,
                    $"Base address should not contain a separator symbol [{string.Join(',', prohibitedCharsBase)}]"));
            }

            if (!string.IsNullOrEmpty(addressExtension))
            {
                var addressExtensionContainsProhibitedChars =
                    addressExtension.IndexOfAny(prohibitedCharsExtension?.ToArray()) != -1;
                if (addressExtensionContainsProhibitedChars)
                {
                    errors.Add(ValidationError.Create(ValidationErrorType.AddressIsNotValid,
                        $"Extension address should not contain a separator [{string.Join(',', prohibitedCharsBase)}]"));
                }
            }

            return errors.Any() ? errors : null;
        }

        private async Task<(bool isAddressExtensionSupported,
                            IReadOnlyCollection<char> prohibitedCharsBase,
                            IEnumerable<char> prohibitedCharsExtension)> IsAddressExtensionSupported(string blockchainType)
        {
            if (!string.IsNullOrEmpty(blockchainType))
            {
                var constants = _blockchainExtensionsService.TryGetAddressExtensionConstants(blockchainType);

                if (constants == null)
                    throw new InvalidOperationException($"{nameof(IsAddressExtensionSupported)}: " +
                                                        "Unable to obtain constants of address extension " +
                                                        "for blockchain type = " +
                                                        $"{blockchainType}.");

                char[] prohibitedSymbols = new[] { constants.Separator };
                return (constants.TypeForWithdrawal == AddressExtensionTypeForWithdrawal.Optional,
                    prohibitedSymbols,
                    prohibitedSymbols);
            }

            return (false, null, null);
        }
    }
}
