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
    /// -> WalletCreatedEvent
    ///     -> BeginTransactionHistoryMonitoringCommand
    /// -> TransactionHistoryMonitoringBeganEvent
    ///     -> BeginBalanceMonitoringCommand
    /// -> BalanceMonitoringBeganEvent
    /// </summary>
    [UsedImplicitly]
    public class WalletCreationSaga
    {
        private readonly ILog _log;

        public WalletCreationSaga(
            ILog log)
        {
            _log = log.CreateComponentScope(nameof(WalletCreationSaga));
        }

        [UsedImplicitly]
        private Task Handle(WalletCreatedEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(WalletCreatedEvent), evt, "");

            try
            {
                sender.SendCommand
                (
                    new BeginTransactionHistoryMonitoringCommand
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
                _log.WriteError(nameof(WalletCreatedEvent), evt, ex);

                throw;
            }
        }

        [UsedImplicitly]
        private Task Handle(TransactionHistoryMonitoringBeganEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(TransactionHistoryMonitoringBeganEvent), evt, "");

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
                _log.WriteError(nameof(TransactionHistoryMonitoringBeganEvent), evt, ex);

                throw;
            }
        }

        [UsedImplicitly]
        private Task Handle(BalanceMonitoringBeganEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(BalanceMonitoringBeganEvent), evt, "");

            try
            {
                // Reserved for future use

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(BalanceMonitoringBeganEvent), evt, ex);

                throw;
            }
        }
    }
}
