// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Squidex.Hosting;
using Testcontainers.PostgreSql;
using Xunit;

namespace Squidex.Events;

public sealed class PostgresEventStoreFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgresSql =
        new PostgreSqlBuilder()
            .WithReuse(true)
            .WithLabel("reuse-id", "eventstore-postgres")
            .Build();

    private IServiceProvider services;

    public IEventStore Store => services.GetRequiredService<IEventStore>();

    public IServiceProvider Services => services;

    public async Task DisposeAsync()
    {
        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.ReleaseAsync(default);
        }

        await postgresSql.StopAsync();
    }

    public async Task InitializeAsync()
    {
        await postgresSql.StartAsync();

        services = new ServiceCollection()
            .AddDbContext<TestContext>(b =>
            {
               b.UseNpgsql(postgresSql.GetConnectionString());
            })
            .AddEntityFrameworkEventStore<TestContext>(TestHelpers.Configuration)
            .AddPostgresAdapter()
            .Services
            .BuildServiceProvider();

        var factory = services.GetRequiredService<IDbContextFactory<TestContext>>();
        var context = await factory.CreateDbContextAsync();
        var creator = (RelationalDatabaseCreator)context.Database.GetService<IDatabaseCreator>();

        await creator.EnsureCreatedAsync();

        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.InitializeAsync(default);
        }
    }
}
