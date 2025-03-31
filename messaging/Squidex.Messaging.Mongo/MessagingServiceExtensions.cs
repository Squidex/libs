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
    public static MessagingBuilder AddMongoTransport(this MessagingBuilder builder, IConfiguration config, Action<MongoTransportOptions>? configure = null,
        string configPath = "messaging:mongoDb")
    {
        builder.Services.ConfigureAndValidate(config, configPath, configure);

        builder.Services.AddSingletonAs<MongoTransport>()
            .As<IMessagingTransport>();

        return builder;
    }

    public static MessagingBuilder AddMongoDataStore(this MessagingBuilder builder, IConfiguration config, Action<MongoMessagingDataOptions>? configure = null,
        string configPath = "messaging:mongoDb:subscriptions")
    {
        builder.Services.ConfigureAndValidate(config, configPath, configure);

        builder.Services.AddSingletonAs<MongoMessagingDataStore>()
            .As<IMessagingDataStore>();

        return builder;
    }
}
