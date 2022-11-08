// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.Messaging;
using Squidex.Messaging.Mongo;

namespace Microsoft.Extensions.DependencyInjection;

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
            .As<IMessagingTransport>();

        services.AddSingletonAs<MongoSubscriptionStore>()
            .As<IMessagingSubscriptionStore>();

        return services;
    }
}
