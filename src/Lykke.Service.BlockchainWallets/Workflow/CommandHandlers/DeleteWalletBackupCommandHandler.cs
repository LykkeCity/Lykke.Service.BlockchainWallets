using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Workflow.Commands;

namespace Lykke.Service.BlockchainWallets.Workflow.CommandHandlers
{
    public class DeleteWalletBackupCommandHandler
    {
        private readonly IBlockchainWalletsBackupRepository _blockchainWalletsBackupRepository;

        public DeleteWalletBackupCommandHandler(IBlockchainWalletsBackupRepository blockchainWalletsBackupRepository)
        {
            _blockchainWalletsBackupRepository = blockchainWalletsBackupRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(DeleteWalletBackupCommand command,
            IEventPublisher publisher)
        {
            await _blockchainWalletsBackupRepository.DeleteIfExistAsync(command.BlockchainType, 
                command.ClientId,
                command.Address);

            return CommandHandlingResult.Ok();
        }
    }
}
