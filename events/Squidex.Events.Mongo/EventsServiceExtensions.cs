// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.Events;
using Squidex.Events.EntityFramework;
using Squidex.Events.Mongo;

namespace Microsoft.Extensions.DependencyInjection;

public static class EventsServiceExtensions
{
    public static EventStoreBuilder AddMongoEventStore(this IServiceCollection services, IConfiguration config, Action<MongoEventStoreOptions>? configure = null,
        string configPath = "eventStore:ef")
    {
        services.Configure(config, configPath, configure);

        services.AddSingletonAs<MongoEventStore>()
            .As<IEventStore>();
        services.AddSingleton(TimeProvider.System);

        return new EventStoreBuilder(services);
    }
}
