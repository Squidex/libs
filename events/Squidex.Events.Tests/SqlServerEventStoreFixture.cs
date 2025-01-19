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
using Testcontainers.MsSql;
using Xunit;

namespace Squidex.Events;

public sealed class SqlServerEventStoreFixture : IAsyncLifetime
{
    private readonly MsSqlContainer msSql =
        new MsSqlBuilder()
            .WithReuse(true)
            .WithLabel("reuse-id", "eventstore-sqlserver").Build();

    public IServiceProvider Services { get; private set; }

    public IEventStore Store => Services.GetRequiredService<IEventStore>();

    public async Task InitializeAsync()
    {
        await msSql.StartAsync();

        Services = new ServiceCollection()
            .AddDbContext<TestContext>(b =>
            {
                b.UseSqlServer(msSql.GetConnectionString());
            })
            .AddEntityFrameworkEventStore<TestContext>(TestHelpers.Configuration, options =>
            {
                options.PollingInterval = TimeSpan.FromSeconds(0.1);
            })
            .AddSqlServerAdapter()
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

        await msSql.StopAsync();
    }
}
