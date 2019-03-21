using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Service.BlockchainWallets.Core.Repositories;
using Lykke.Service.BlockchainWallets.Workflow.Commands;

namespace Lykke.Service.BlockchainWallets.Workflow.CommandHandlers
{
    public class SetPrimaryWalletBackupCommandHandler
    {
        private readonly IBlockchainWalletsBackupRepository _blockchainWalletsBackupRepository;

        public SetPrimaryWalletBackupCommandHandler(IBlockchainWalletsBackupRepository blockchainWalletsBackupRepository)
        {
            _blockchainWalletsBackupRepository = blockchainWalletsBackupRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(SetPrimaryWalletBackupCommand command,
            IEventPublisher publisher)
        {
            await _blockchainWalletsBackupRepository.SetPrimaryWalletAsync(command.BlockchainType,
                command.ClientId,
                command.Address,
                command.Version);

            return CommandHandlingResult.Ok();
        }
    }
}
