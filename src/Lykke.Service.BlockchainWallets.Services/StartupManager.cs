using System;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Service.BlockchainWallets.Core.Services;

namespace Lykke.Service.BlockchainWallets.Services
{
    public class StartupManager : IStartupManager
    {
        private readonly ILog _log;
        private readonly IBlockchainExtensionsService _blockchainExtensionsService;
        private readonly ICqrsEngine _cqrsEngine;

        public StartupManager(
            ILogFactory logFactory,
            IBlockchainExtensionsService capabilitiesService,
            ICqrsEngine cqrsEngine)
        {
            if (logFactory == null)
                throw new ArgumentNullException(nameof(logFactory));
            _log = logFactory.CreateLog(this);

            _blockchainExtensionsService = capabilitiesService ?? throw new ArgumentNullException(nameof(capabilitiesService));
            _cqrsEngine = cqrsEngine;
        }

        public void Start()
        {
            _log.Info("Starting initialization of the services...");

            _log.Info("Initializaton attempt for Blockchain Extensions...");
            _blockchainExtensionsService.FireInitializationAndForget();
            _cqrsEngine.Start();
            _log.Info("Service initialization (attempts) finished.");
        }
    }
}
