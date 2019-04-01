using JetBrains.Annotations;
using Lykke.AzureStorage.Tables;
using Lykke.Service.BlockchainWallets.Core.DTOs.Validation;

namespace Lykke.Service.BlockchainWallets.AzureRepositories.Entities
{
    internal class BlackListEntity : AzureTableEntity
    {
        #region Fields

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string BlockchainIntegrationLayerId { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string BlockedAddressLowCase { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string BlockedAddress { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool IsCaseSensitive { get; set; }

        #endregion


        #region Keys

        public static string GetPartitionKey(string blockchainIntegrationLayerId)
        {
            return blockchainIntegrationLayerId;
        }

        public static string GetRowKey(string blockedAddress)
        {
            return blockedAddress.ToLower();
        }

        #endregion


        #region Conversion

        public static BlackListEntity FromDomain(BlackListModel model)
        {
            return new BlackListEntity
            {
                PartitionKey = GetPartitionKey(model.BlockchainType),
                RowKey = GetRowKey(model.BlockedAddress),
                BlockchainIntegrationLayerId = model.BlockchainType,
                BlockedAddress = model.BlockedAddress,
                BlockedAddressLowCase = model.BlockedAddress?.ToLower(),
                IsCaseSensitive = model.IsCaseSensitive,
            };
        }

        public BlackListModel ToDomain()
        {
            return new BlackListModel(this.BlockchainIntegrationLayerId, this.BlockedAddress, this.IsCaseSensitive);
        }

        #endregion
    }
}
