using System.Collections.Generic;
using Autofac;
using Common.Log;
using Inceptum.Cqrs.Configuration;
using Inceptum.Messaging;
using Inceptum.Messaging.Contract;
using Inceptum.Messaging.RabbitMq;
using Lykke.Cqrs;
using Lykke.Messaging;
using Lykke.Service.BlockchainWallets.Core.Domain.Wallet;
using Lykke.Service.BlockchainWallets.Core.Domain.Wallet.Commands;
using Lykke.Service.BlockchainWallets.Core.Settings.ServiceSettings;
using Lykke.Service.BlockchainWallets.Workflow;
using Lykke.Service.BlockchainWallets.Workflow.CommandHandlers;

namespace Lykke.Service.BlockchainWallets.Modules
{
    public class CqrsModule : Module
    {
        private readonly CqrsSettings _settings;
        private readonly ILog _log;

        public CqrsModule(CqrsSettings settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder
                .Register(ctx => new AutofacDependencyResolver(ctx))
                .As<IDependencyResolver>()
                .SingleInstance();

            builder
                .RegisterType<RetryDelayProvider>()
                .AsSelf()
                .WithParameter(TypedParameter.From(_settings.RetryDelay))
                .SingleInstance();

            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = _settings.RabbitConnectionString
            };

            var transportInfo = new TransportInfo
            (
                rabbitMqSettings.Endpoint.ToString(),
                rabbitMqSettings.UserName,
                rabbitMqSettings.Password,
                "None",
                "RabbitMq"
            );

            var transports = new Dictionary<string, TransportInfo>
            {
                {"RabbitMq", transportInfo}
            };

            var messagingEngine = new MessagingEngine
            (
                _log,
                new TransportResolver(transports),
                new RabbitMqTransportFactory()
            );

            // Command handlers
            builder
                .RegisterType<BeginBalanceMonitoringCommandHandler>();

            builder
                .RegisterType<EndBalanceMonitoringCommandHandler>();

            // Create engine
            builder.Register(ctx => CreateEngine(ctx, messagingEngine))
                .As<ICqrsEngine>()
                .SingleInstance()
                .AutoActivate();
        }

        private CqrsEngine CreateEngine(IComponentContext ctx, IMessagingEngine messagingEngine)
        {
            var defaultRetryDelay = (long) _settings.RetryDelay.TotalMilliseconds;

            var enpointResolverRegistration = Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver
            (
                transport: "RabbitMq",
                serializationFormat: "protobuf",
                environment: "lykke"
            ));

            const string defaultPipeline = "commands";
            const string defaultRoute = "self";

            var boundedContextRegistration = Register.BoundedContext(WalletsBoundedContext.Name)
                .FailedCommandRetryDelay(defaultRetryDelay)

                .ListeningCommands(typeof(BeginBalanceMonitoringCommand))
                .On(defaultRoute)
                .WithCommandsHandler<BeginBalanceMonitoringCommandHandler>()

                .ListeningCommands(typeof(EndBalanceMonitoringCommand))
                .On(defaultRoute)
                .WithCommandsHandler<EndBalanceMonitoringCommandHandler>()

                .ProcessingOptions(defaultRoute).MultiThreaded(8).QueueCapacity(1024);

            return new CqrsEngine
            (
                log: _log,
                dependencyResolver: ctx.Resolve<IDependencyResolver>(),
                messagingEngine: messagingEngine,
                endpointProvider: new DefaultEndpointProvider(),
                createMissingEndpoints: true,
                registrations: new IRegistration[]
                {
                    enpointResolverRegistration,
                    boundedContextRegistration
                }
            );
        }
    }
}
