// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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

    public IServiceProvider Services { get; private set; }

    public IEventStore Store => Services.GetRequiredService<IEventStore>();

    public async Task InitializeAsync()
    {
        await postgresSql.StartAsync();

        Services = new ServiceCollection()
            .AddDbContextFactory<TestContext>(b =>
            {
               b.UseNpgsql(postgresSql.GetConnectionString());
            })
            .AddEntityFrameworkEventStore<TestContext>(TestHelpers.Configuration, options =>
            {
                options.PollingInterval = TimeSpan.FromSeconds(0.1);
            })
            .AddPostgresAdapter()
            .Services
            .BuildServiceProvider();

        var factory = Services.GetRequiredService<IDbContextFactory<TestContext>>();
        var context = await factory.CreateDbContextAsync();
        var creator = (RelationalDatabaseCreator)context.Database.GetService<IDatabaseCreator>();

        await creator.EnsureCreatedAsync();

        foreach (var service in Services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.InitializeAsync(default);
        }
    }

    public async Task DisposeAsync()
    {
        foreach (var service in Services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.ReleaseAsync(default);
        }

        await postgresSql.StopAsync();
    }
}
