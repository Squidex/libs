// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.Messaging;
using Squidex.Messaging.Implementation;
using Squidex.Messaging.Mongo;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MessagingServiceExtensions
    {
        public static IServiceCollection AddMongoTransport(this IServiceCollection services, IConfiguration config, Action<MongoTransportOptions>? configure = null)
        {
            services.ConfigureAndValidate<MongoTransportOptions>(config, "messaging:mongoDb");
            services.ConfigureAndValidate<MongoSubscriptionStoreOptions>(config, "messaging:mongoDb:subscriptions");

            if (configure != null)
            {
                services.Configure(configure);
            }

            services.AddSingletonAs<MongoTransport>()
                .As<ITransport>();

            services.AddSingletonAs<MongoSubscriptionStore>()
                .As<ISubscriptionStore>();

            return services;
        }
    }
}
