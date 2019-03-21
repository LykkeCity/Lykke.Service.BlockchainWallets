using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Contract.Events;
using Lykke.Service.BlockchainWallets.Workflow.Commands;

namespace Lykke.Service.BlockchainWallets.Workflow.Sagas
{
    [UsedImplicitly]
    public class PrimaryWalletHandlingSaga
    {
        [UsedImplicitly]
        private async Task Handle(PrimaryWalletChangedEvent evt, ICommandSender sender)
        {
            sender.SendCommand(new SetPrimaryWalletBackupCommand
            {
                ClientId = evt.ClientId,
                Address = evt.Address,
                Version = evt.Version,
                BlockchainType = evt.BlockchainType
            },BlockchainWalletsBoundedContext.Name);
        }
    }
}
