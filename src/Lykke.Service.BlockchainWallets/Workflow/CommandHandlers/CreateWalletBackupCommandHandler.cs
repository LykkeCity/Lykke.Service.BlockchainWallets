using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Workflow.Commands;

namespace Lykke.Service.BlockchainWallets.Workflow.CommandHandlers
{
    public class CreateWalletBackupCommandHandler
    {
        private readonly IBlockchainWalletsBackupRepository _blockchainWalletsBackupRepository;

        public CreateWalletBackupCommandHandler(IBlockchainWalletsBackupRepository blockchainWalletsBackupRepository)
        {
            _blockchainWalletsBackupRepository = blockchainWalletsBackupRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(CreateWalletBackupCommand command,
            IEventPublisher publisher)
        {
            await _blockchainWalletsBackupRepository.AddAsync(command.BlockchainType, command.ClientId, command.Address,
                command.CreatedBy, command.IsPrimary);

            return CommandHandlingResult.Ok();
        }
    }
}
