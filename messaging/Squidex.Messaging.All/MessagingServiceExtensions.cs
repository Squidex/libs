// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.Messaging;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessagingServiceExtensions
{
    public static MessagingBuilder AddTransport(this MessagingBuilder builder, IConfiguration config, Alternatives? custom = null)
    {
        var options = new Alternatives
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
            ["RabbitMq"] = () =>
            {
                builder.AddRabbitMqTransport(config);
            },
            ["Redis"] = () =>
            {
                builder.AddRedisTransport(config);
            },
        };

        if (custom != null)
        {
            foreach (var (key, configure) in custom)
            {
                options[key] = configure;
            }
        }

        config.ConfigureByOption("messaging:type", options);

        return builder;
    }
}
