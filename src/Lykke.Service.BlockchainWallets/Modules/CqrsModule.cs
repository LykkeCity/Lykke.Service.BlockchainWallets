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
using Lykke.Service.BlockchainWallets.Core.Settings.ServiceSettings;
using Lykke.Service.BlockchainWallets.Workflow.CommandHandlers;
using Lykke.Service.BlockchainWallets.Workflow.Commands;
using Lykke.Service.BlockchainWallets.Workflow.Sagas;
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
                .RegisterType<BeginTransactionHistoryMonitoringCommandHandler>();

            builder
                .RegisterType<EndBalanceMonitoringCommandHandler>();

            builder
                .RegisterType<EndTransactionHistoryMonitoringCommandHandler>();

            // Sagas

            builder
                .RegisterType<WalletSubscriptionSaga>();

            builder
                .RegisterType<WalletUnsubscriptionSaga>();

            // Create engine
            builder.Register(ctx => CreateEngine(ctx, messagingEngine))
                .As<ICqrsEngine>()
                .SingleInstance()
                .AutoActivate();
        }

        private CqrsEngine CreateEngine(IComponentContext ctx, IMessagingEngine messagingEngine)
        {
            const string defaultPipeline = "commands";
            const string defaultRoute = "self";

            var defaultRetryDelay = (long) _settings.RetryDelay.TotalMilliseconds;
            
            var registrations = new IRegistration[]
            {
                Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver
                (
                    "RabbitMq",
                    "messagepack",
                    environment: "lykke"
                )),

                Register.BoundedContext(BlockchainWalletsBoundedContext.Name)
                .FailedCommandRetryDelay(defaultRetryDelay)

                .ListeningCommands(typeof(BeginBalanceMonitoringCommand))
                .On(defaultRoute)
                .WithCommandsHandler<BeginBalanceMonitoringCommandHandler>()

                .ListeningCommands(typeof(EndBalanceMonitoringCommand))
                .On(defaultRoute)
                .WithCommandsHandler<EndBalanceMonitoringCommandHandler>()

                .PublishingCommands(
                    typeof(BeginBalanceMonitoringCommand),
                    typeof(BeginTransactionHistoryMonitoringCommand),
                    typeof(EndBalanceMonitoringCommand),
                    typeof(EndTransactionHistoryMonitoringCommand))
                .To(BlockchainWalletsBoundedContext.Name)
                .With(defaultRoute)

                .PublishingEvents(
                    typeof(WalletCreatedEvent),
                    typeof(WalletDeletedEvent))
                .With(BlockchainWalletsBoundedContext.EventsRoute)

                .ProcessingOptions(defaultRoute).MultiThreaded(8).QueueCapacity(1024),

            Register.Saga<WalletSubscriptionSaga>($"{BlockchainWalletsBoundedContext.Name}.wallet-creation-saga")
                .ListeningEvents(
                    typeof(WalletCreatedEvent))
                .From(BlockchainWalletsBoundedContext.Name)
                .On(defaultRoute)
                .PublishingCommands(
                    typeof(BeginBalanceMonitoringCommand),
                    typeof(BeginTransactionHistoryMonitoringCommand))
                .To(BlockchainWalletsBoundedContext.Name)
                .With(defaultPipeline)

                .ProcessingOptions(defaultRoute).MultiThreaded(8).QueueCapacity(1024),

            Register.Saga<WalletUnsubscriptionSaga>($"{BlockchainWalletsBoundedContext.Name}.wallet-deletion-saga")
                .ListeningEvents(
                    typeof(WalletDeletedEvent))
                .From(BlockchainWalletsBoundedContext.Name)
                .On(defaultRoute)
                .PublishingCommands(
                    typeof(EndBalanceMonitoringCommand),
                    typeof(EndTransactionHistoryMonitoringCommand))
                .To(BlockchainWalletsBoundedContext.Name)
                .With(defaultPipeline)

                .ProcessingOptions(defaultRoute).MultiThreaded(8).QueueCapacity(1024)
            };
            
            return new CqrsEngine
            (
                _log,
                ctx.Resolve<IDependencyResolver>(),
                messagingEngine,
                new DefaultEndpointProvider(),
                true, registrations);
        }
    }
}
