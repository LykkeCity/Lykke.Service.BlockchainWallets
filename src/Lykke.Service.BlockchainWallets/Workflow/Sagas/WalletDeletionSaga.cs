using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Contract.Events;
using Lykke.Service.BlockchainWallets.Workflow.Commands;
using Lykke.Service.BlockchainWallets.Workflow.Events;


namespace Lykke.Service.BlockchainWallets.Workflow.Sagas
{
    /// <summary>
    /// -> WalletDeletedEvent
    ///     -> EndTransactionHistoryMonitoringCommand
    /// -> TransactionHistoryMonitoringEndedEvent
    ///     -> EndBalanceMonitoringCommand
    /// -> BalanceMonitoringEndedEvent
    /// </summary>
    [UsedImplicitly]
    public class WalletDeletionSaga
    {
        private readonly ILog _log;

        public WalletDeletionSaga(
            ILog log)
        {
            _log = log.CreateComponentScope(nameof(WalletCreationSaga));
        }

        [UsedImplicitly]
        private Task Handle(WalletDeletedEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(WalletDeletedEvent), evt, "");

            try
            {
                sender.SendCommand
                (
                    new EndTransactionHistoryMonitoringCommand
                    {
                        Address = evt.Address,
                        AssetId = evt.AssetId,
                        BlockchainType = evt.IntegrationLayerId
                    },
                    BlockchainWalletsBoundedContext.Name
                );

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(WalletDeletedEvent), evt, ex);

                throw;
            }
        }

        [UsedImplicitly]
        private Task Handle(TransactionHistoryMonitoringEndedEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(TransactionHistoryMonitoringEndedEvent), evt, "");

            try
            {
                sender.SendCommand
                (
                    new EndBalanceMonitoringCommand
                    {
                        Address = evt.Address,
                        AssetId = evt.AssetId,
                        BlockchainType = evt.BlockchainType
                    },
                    BlockchainWalletsBoundedContext.Name
                );

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(TransactionHistoryMonitoringEndedEvent), evt, ex);

                throw;
            }
        }

        [UsedImplicitly]
        private Task Handle(BalanceMonitoringEndedEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(BalanceMonitoringEndedEvent), evt, "");

            try
            {
                // Reserved for future use

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(BalanceMonitoringEndedEvent), evt, ex);

                throw;
            }
        }
    }
}
