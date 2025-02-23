// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Squidex.Assets.EntityFramework;
using Squidex.Hosting;
using Testcontainers.PostgreSql;
using Xunit;

namespace Squidex.Assets.KeyValueStore;

public class PostgresKeyValueStoreFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgresSql =
        new PostgreSqlBuilder()
            .WithReuse(true)
            .WithLabel("reuse-id", "assets-kvp-postgres")
            .Build();

    public class TestContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseAssetKeyValueStore<TestValue>();
            base.OnModelCreating(modelBuilder);
        }
    }

    public IServiceProvider Services { get; private set; }

    public EFAssetKeyValueStore<TestContext, TestValue> Store => Services.GetRequiredService<EFAssetKeyValueStore<TestContext, TestValue>>();

    public async Task InitializeAsync()
    {
        await postgresSql.StartAsync();

        Services = new ServiceCollection()
            .AddDbContextFactory<TestContext>(b =>
            {
                b.UseNpgsql(postgresSql.GetConnectionString());
            })
            .AddEntityFrameworkAssetKeyValueStore<TestContext, TestValue>()
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
