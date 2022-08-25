// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MessagingServiceExtensions
    {
        public static IServiceCollection AddMessagingTransport(this IServiceCollection services, IConfiguration config)
        {
            config.ConfigureByOption("messaging:type", new Alternatives
            {
                ["MongoDb"] = () =>
                {
                    services.AddMongoTransport(config);
                },
                ["Scheduler"] = () =>
                {
                    services.AddMongoTransport(config);
                },
                ["GooglePubSub"] = () =>
                {
                    services.AddGooglePubSubTransport(config);
                },
                ["Kafka"] = () =>
                {
                    services.AddKafkaTransport(config);
                },
                ["RabbitMq"] = () =>
                {
                    services.AddRabbitMqTransport(config);
                },
                ["Redis"] = () =>
                {
                    services.AddRedisTransport(config);
                }
            });

            return services;
        }
    }
}
