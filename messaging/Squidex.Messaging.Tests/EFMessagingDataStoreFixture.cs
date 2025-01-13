// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

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
            .WithReuse(true)
            .WithLabel("reuse-id", "messagingstore-kafka")
            .Build();

    private IServiceProvider services;

    public IMessagingDataStore Store => services.GetRequiredService<IMessagingDataStore>();

    public sealed class AppDbContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddMessagingDataStore();
            base.OnModelCreating(modelBuilder);
        }
    }

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
            .AddDbContext<AppDbContext>(b =>
            {
                b.UseNpgsql(postgresSql.GetConnectionString());
            })
            .AddLogging()
            .AddMessaging()
            .AddEntityFrameworkDataStore<AppDbContext>(TestHelpers.Configuration)
            .Services
            .BuildServiceProvider();

        var factory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        var context = await factory.CreateDbContextAsync();
        var creator = (RelationalDatabaseCreator)context.Database.GetService<IDatabaseCreator>();

        await creator.EnsureCreatedAsync();

        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.InitializeAsync(default);
        }
    }
}
