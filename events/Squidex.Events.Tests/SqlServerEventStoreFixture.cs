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
using Testcontainers.MsSql;
using Xunit;

namespace Squidex.Events;

public sealed class SqlServerEventStoreFixture : IAsyncLifetime
{
    private readonly MsSqlContainer msSql =
        new MsSqlBuilder()
            .WithReuse(true)
            .WithLabel("reuse-id", "eventstore-sqlserver").Build();

    private IServiceProvider services;

    public IEventStore Store => services.GetRequiredService<IEventStore>();

    public IServiceProvider Services => services;

    public async Task DisposeAsync()
    {
        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.ReleaseAsync(default);
        }

        await msSql.StopAsync();
    }

    public async Task InitializeAsync()
    {
        await msSql.StartAsync();

        services = new ServiceCollection()
            .AddDbContext<TestContext>(b =>
            {
                b.UseSqlServer(msSql.GetConnectionString());
            })
            .AddEntityFrameworkEventStore<TestContext>(TestHelpers.Configuration)
            .AddSqlServerAdapter()
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
