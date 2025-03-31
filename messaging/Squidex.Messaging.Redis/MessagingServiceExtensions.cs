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
    public static MessagingBuilder AddRedisTransport(this MessagingBuilder builder, IConfiguration config, Action<RedisTransportOptions>? configure = null,
        string configPath = "messaging:redis")
    {
        builder.Services.ConfigureAndValidate(config, configPath, configure);

        builder.Services.AddSingletonAs<RedisTransport>()
            .As<IMessagingTransport>();

        return builder;
    }
}
