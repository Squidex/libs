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
using Testcontainers.MySql;
using Xunit;

namespace Squidex.Events;

public sealed class MySqlEventStoreFixture : IAsyncLifetime
{
    private readonly MySqlContainer mysql =
        new MySqlBuilder()
            .WithReuse(true)
            .WithLabel("reuse-id", "eventstore-mysql")
            .WithUsername("root")
            .Build();

    public IServiceProvider Services { get; private set; }

    public IEventStore Store => Services.GetRequiredService<IEventStore>();

    public async Task InitializeAsync()
    {
        await mysql.StartAsync();

        Services = new ServiceCollection()
            .AddDbContextFactory<TestContext>(b =>
            {
                b.UseMySql(mysql.GetConnectionString(), ServerVersion.AutoDetect(mysql.GetConnectionString()));
            })
            .AddEntityFrameworkEventStore<TestContext>(TestHelpers.Configuration, options =>
            {
                options.PollingInterval = TimeSpan.FromSeconds(0.1);
            })
            .AddMysqlAdapter()
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

        await mysql.StopAsync();
    }
}
