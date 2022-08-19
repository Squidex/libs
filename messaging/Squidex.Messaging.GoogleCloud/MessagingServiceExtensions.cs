// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.Messaging;
using Squidex.Messaging.GoogleCloud;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MessagingServiceExtensions
    {
        public static IServiceCollection AddGooglePubSubTransport(this IServiceCollection services, IConfiguration config, Action<GooglePubSubTransportOptions>? configure = null)
        {
            services.ConfigureAndValidate<GooglePubSubTransportOptions>(config, "messaging:googlePubSub");

            if (configure != null)
            {
                services.Configure(configure);
            }

            services.AddSingletonAs<GooglePubSubTransport>()
                .As<ITransport>();

            return services;
        }
    }
}
