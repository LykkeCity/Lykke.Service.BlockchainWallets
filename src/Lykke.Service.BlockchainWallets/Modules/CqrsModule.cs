using System.Collections.Generic;
using Autofac;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Messaging;
using Lykke.Messaging.Contract;
using Lykke.Messaging.RabbitMq;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Contract.Events;
using Lykke.Service.BlockchainWallets.Core.Domain.Wallet.Commands;
using Lykke.Service.BlockchainWallets.Core.Settings.ServiceSettings;
using Lykke.Service.BlockchainWallets.Workflow.CommandHandlers;
using RabbitMQ.Client;

namespace Lykke.Service.BlockchainWallets.Modules
{
    public class CqrsModule : Module
    {
        private readonly ILog _log;
        private readonly CqrsSettings _settings;

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

            var rabbitMqSettings = new ConnectionFactory
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
                "RabbitMq",
                "messagepack",
                environment: "lykke"
            ));
            

            const string defaultRoute = "self";

            var boundedContextRegistration = Register.BoundedContext(BlockchainWalletsBoundedContext.Name)
                .FailedCommandRetryDelay(defaultRetryDelay)
                .ListeningCommands(typeof(BeginBalanceMonitoringCommand))
                .On(defaultRoute)

                .WithCommandsHandler<BeginBalanceMonitoringCommandHandler>()
                .ListeningCommands(typeof(EndBalanceMonitoringCommand))
                .On(defaultRoute)

                .WithCommandsHandler<EndBalanceMonitoringCommandHandler>()
                .PublishingCommands(typeof(BeginBalanceMonitoringCommand), typeof(EndBalanceMonitoringCommand))
                .To(BlockchainWalletsBoundedContext.Name)
                .With(defaultRoute)

                .PublishingEvents(typeof(WalletCreatedEvent), typeof(WalletDeletedEvent))
                .With(BlockchainWalletsBoundedContext.EventsRoute)

                .ProcessingOptions(defaultRoute).MultiThreaded(8).QueueCapacity(1024);

            return new CqrsEngine
            (
                _log,
                ctx.Resolve<IDependencyResolver>(),
                messagingEngine,
                new DefaultEndpointProvider(),
                true, enpointResolverRegistration, boundedContextRegistration);
        }
    }
}
