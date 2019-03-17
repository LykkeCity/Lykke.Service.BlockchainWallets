using System.Collections.Generic;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Messaging;
using Lykke.Messaging.Contract;
using Lykke.Messaging.RabbitMq;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Contract.Events;
using Lykke.Service.BlockchainWallets.Workflow.Commands;
using RabbitMQ.Client;
using SerializationFormat = Lykke.Messaging.Serialization.SerializationFormat;

namespace Lykke.Service.BlockchainWallets.ObsoleteAzureToMongoMigrator.Cqrs
{
    public static class CqrsBuilder
    {
        public static CqrsEngine CreateCqrsEngine(string connectionString, ILogFactory logFactory)
        {
            var rabbitMqSettings = new ConnectionFactory
            {
                Uri = connectionString
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
            return CreateEngine(
                logFactory,
                new MessagingEngine(
                    logFactory,
                    new TransportResolver(transports),
                    new RabbitMqTransportFactory(logFactory)
                )
            );
        }

        private static CqrsEngine CreateEngine(ILogFactory logFactory, IMessagingEngine messagingEngine)
        {
            var registrations = new IRegistration[]
            {
                Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver
                (
                    "RabbitMq",
                    SerializationFormat.MessagePack,
                    environment: "lykke"
                )),

                Register.BoundedContext(BlockchainWalletsBoundedContext.Name)
                    .PublishingCommands(typeof(CreateWalletBackupCommand))
                    .To(BlockchainWalletsBoundedContext.Name)
                    .With("commands")
                    
            };

            return new CqrsEngine
            (
                logFactory,
                null,
                messagingEngine,
                new DefaultEndpointProvider(),
                true,
                registrations);
        }
    }
}
