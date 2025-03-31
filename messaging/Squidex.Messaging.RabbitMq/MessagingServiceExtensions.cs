// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.Messaging;
using Squidex.Messaging.RabbitMq;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessagingServiceExtensions
{
    public static MessagingBuilder AddRabbitMqTransport(this MessagingBuilder builder, IConfiguration config, Action<RabbitMqTransportOptions>? configure = null,
        string configPath = "messaging:rabbitMq")
    {
        builder.Services.ConfigureAndValidate(config, configPath, configure);

        builder.Services.AddSingletonAs<RabbitMqOwner>()
            .AsSelf();

        builder.Services.AddSingletonAs<RabbitMqTransport>()
            .As<IMessagingTransport>();

        return builder;
    }
}
