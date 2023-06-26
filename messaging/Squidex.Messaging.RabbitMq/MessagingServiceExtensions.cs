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
    public static IServiceCollection AddRabbitMqTransport(this IServiceCollection services, IConfiguration config, Action<RabbitMqTransportOptions>? configure = null,
        string configPath = "messaging:rabbitMq")
    {
        services.ConfigureAndValidate(config, configPath, configure);

        services.AddSingletonAs<RabbitMqOwner>()
            .AsSelf();

        services.AddSingletonAs<RabbitMqTransport>()
            .As<IMessagingTransport>();

        return services;
    }
}
