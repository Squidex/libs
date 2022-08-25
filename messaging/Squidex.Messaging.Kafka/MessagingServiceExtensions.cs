// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.Messaging;
using Squidex.Messaging.Kafka;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MessagingServiceExtensions
    {
        public static IServiceCollection AddKafkaTransport(this IServiceCollection services, IConfiguration config, Action<KafkaTransportOptions>? configure = null)
        {
            services.ConfigureAndValidate<KafkaTransportOptions>(config, "messaging:kafka");

            if (configure != null)
            {
                services.Configure(configure);
            }

            services.AddSingletonAs<KafkaOwner>()
                .AsSelf();

            services.AddSingletonAs<KafkaTransport>()
                .As<IMessagingTransport>();

            return services;
        }
    }
}
