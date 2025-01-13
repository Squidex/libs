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
using Testcontainers.MariaDb;
using Xunit;

namespace Squidex.Events;

public sealed class MariaDbEventStoreFixture : IAsyncLifetime
{
    private readonly MariaDbContainer mariaDb =
        new MariaDbBuilder()
            .WithReuse(true)
            .WithLabel("reuse-id", "eventstore-mariadb")
            .WithUsername("root")
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

        await mariaDb.StopAsync();
    }

    public async Task InitializeAsync()
    {
        await mariaDb.StartAsync();

        services = new ServiceCollection()
            .AddDbContext<TestContext>(b =>
            {
                b.UseMySql(mariaDb.GetConnectionString(), ServerVersion.AutoDetect(mariaDb.GetConnectionString()));
            })
            .AddEntityFrameworkEventStore<TestContext>(TestHelpers.Configuration)
            .AddMysqlAdapter()
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
