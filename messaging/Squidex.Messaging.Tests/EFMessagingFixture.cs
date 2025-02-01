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
using Testcontainers.PostgreSql;
using Xunit;

namespace Squidex.Messaging;

public sealed class EFMessagingFixture : IAsyncLifetime
{
    public PostgreSqlContainer PostgresSql { get; } =
        new PostgreSqlBuilder()
            .WithReuse(Debugger.IsAttached)
            .WithLabel("reuse-id", "messaging-postgres")
            .Build();

    public sealed class TestContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddMessagingDataStore();
            modelBuilder.AddMessagingTransport();
            base.OnModelCreating(modelBuilder);
        }
    }

    public async Task InitializeAsync()
    {
        await PostgresSql.StartAsync();

        var services = new ServiceCollection()
            .AddDbContextFactory<TestContext>(b =>
            {
                b.UseNpgsql(PostgresSql.GetConnectionString());
            })
            .BuildServiceProvider();

        var factory = services.GetRequiredService<IDbContextFactory<TestContext>>();
        var context = await factory.CreateDbContextAsync();
        var creator = (RelationalDatabaseCreator)context.Database.GetService<IDatabaseCreator>();

        await creator.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await PostgresSql.StopAsync();
    }
}
