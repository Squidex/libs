// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.Messaging;
using Squidex.Messaging.Redis;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessagingServiceExtensions
{
    public static IServiceCollection AddRedisTransport(this IServiceCollection services, IConfiguration config, Action<RedisTransportOptions>? configure = null,
        string configPath = "messaging:redis")
    {
        services.ConfigureAndValidate(config, configPath, configure);

        services.AddSingletonAs<RedisTransport>()
            .As<IMessagingTransport>();

        return services;
    }
}
