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
    public static IServiceCollection AddMongoTransport(this IServiceCollection services, IConfiguration config, Action<MongoTransportOptions>? configure = null,
        string configPath = "messaging:mongoDb")
    {
        services.ConfigureAndValidate(config, configPath, configure);

        services.AddSingletonAs<MongoTransport>()
            .As<IMessagingTransport>();

        return services;
    }

    public static IServiceCollection AddMongoSubscriptions(this IServiceCollection services, IConfiguration config, Action<MongoSubscriptionStoreOptions>? configure = null,
        string configPath = "messaging:mongoDb:subscriptions")
    {
        services.ConfigureAndValidate(config, configPath, configure);

        services.AddSingletonAs<MongoSubscriptionStore>()
            .As<IMessagingSubscriptionStore>();

        return services;
    }
}
