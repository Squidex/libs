// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.Messaging;
using Squidex.Messaging.Redis;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MessagingServiceExtensions
    {
        public static IServiceCollection AddRedisTransport(this IServiceCollection services, IConfiguration config, Action<RedisTransportOptions>? configure = null)
        {
            services.ConfigureAndValidate<RedisTransportOptions>(config, "messaging:redis");

            if (configure != null)
            {
                services.Configure(configure);
            }

            services.AddSingletonAs<RedisTransport>()
                .As<ITransport>();

            return services;
        }
    }
}
