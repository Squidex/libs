// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Squidex.Events;
using Squidex.Events.EntityFramework;
using Squidex.Events.EntityFramework.Mysql;
using Squidex.Events.EntityFramework.Postgres;
using Squidex.Events.EntityFramework.SqlServer;

namespace Microsoft.Extensions.DependencyInjection;

public static class EventsServiceExtensions
{
    public static EventStoreBuilder AddEntityFrameworkEventStore<T>(this IServiceCollection services, IConfiguration config, Action<EFEventStoreOptions>? configure = null,
        string configPath = "eventStore:sql") where T : DbContext
    {
        services.Configure(config, configPath, configure);

        services.AddSingletonAs<EFEventStore<T>>()
            .As<IEventStore>();
        services.AddSingleton(TimeProvider.System);

        return new EventStoreBuilder(services);
    }

    public static EventStoreBuilder AddPostgresAdapter(this EventStoreBuilder builder)
    {
        builder.Services.AddSingletonAs<PostgresAdapter>()
            .As<IProviderAdapter>();

        return builder;
    }

    public static EventStoreBuilder AddMysqlAdapter(this EventStoreBuilder builder)
    {
        builder.Services.AddSingletonAs<MysqlAdapter>()
            .As<IProviderAdapter>();

        return builder;
    }

    public static EventStoreBuilder AddSqlServerAdapter(this EventStoreBuilder builder)
    {
        builder.Services.AddSingletonAs<SqlServerAdapter>()
            .As<IProviderAdapter>();

        return builder;
    }
}
