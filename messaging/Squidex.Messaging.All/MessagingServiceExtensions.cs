// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.Messaging;
using Squidex.Messaging.Implementation;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessagingServiceExtensions
{
    public static MessagingBuilder AddTransport(this MessagingBuilder builder, IConfiguration config)
    {
        config.ConfigureByOption("messaging:type", new Alternatives
        {
            ["MongoDb"] = () =>
            {
                builder.AddMongoTransport(config);
            },
            ["Scheduler"] = () =>
            {
                builder.AddMongoTransport(config);
            },
            ["GooglePubSub"] = () =>
            {
                builder.AddGooglePubSubTransport(config);
            },
            ["Kafka"] = () =>
            {
                builder.AddKafkaTransport(config);
            },
            ["RabbitMq"] = () =>
            {
                builder.AddRabbitMqTransport(config);
            },
            ["Redis"] = () =>
            {
                builder.AddRedisTransport(config);
            }
        });

        return builder;
    }
}
