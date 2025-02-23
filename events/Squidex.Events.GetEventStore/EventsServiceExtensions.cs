// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Squidex.Events.EntityFramework;

namespace Squidex.Events.GetEventStore;

public static class EventsServiceExtensions
{
    public static EventStoreBuilder AddGetEventStore(this IServiceCollection services, IConfiguration config, Action<GetEventStoreOptions>? configure = null,
        string configPath = "eventStore:eventStore")
    {
        services.Configure(config, configPath, configure);

        services.AddSingletonAs<GetEventStore>()
            .As<IEventStore>();
        services.AddSingleton(TimeProvider.System);

        return new EventStoreBuilder(services);
    }
}
