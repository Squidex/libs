// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Squidex.Hosting;
using Testcontainers.PostgreSql;
using Xunit;

namespace Squidex.Messaging;

public class EFMessagingDataStoreFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgresSql =
        new PostgreSqlBuilder()
            .WithReuse(Debugger.IsAttached)
            .WithLabel("reuse-id", "messagingstore-kafka")
            .Build();

    public IServiceProvider Services { get; private set; }

    public IMessagingDataStore Store => Services.GetRequiredService<IMessagingDataStore>();

    public sealed class TestContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseMessagingDataStore();
            base.OnModelCreating(modelBuilder);
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

    public async Task InitializeAsync()
    {
        await postgresSql.StartAsync();

        Services = new ServiceCollection()
            .AddDbContextFactory<TestContext>(b =>
            {
                b.UseNpgsql(postgresSql.GetConnectionString());
            })
            .AddLogging()
            .AddMessaging()
            .AddEntityFrameworkDataStore<TestContext>(TestHelpers.Configuration)
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
}
