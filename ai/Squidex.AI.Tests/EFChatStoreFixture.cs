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
using Microsoft.Extensions.DependencyInjection;
using Squidex.AI.Mongo;
using Squidex.Hosting;
using Testcontainers.PostgreSql;
using Xunit;

namespace Squidex.AI;

public sealed class EFChatStoreFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgresSql =
        new PostgreSqlBuilder()
            .WithReuse(Debugger.IsAttached)
            .WithLabel("reuse-id", "chatstore-postgres")
            .Build();

    public IServiceProvider Services { get; private set; }

    public EFChatStore<AppDbContext> Store => Services.GetRequiredService<EFChatStore<AppDbContext>>();

    public sealed class AppDbContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddChatStore();
            base.OnModelCreating(modelBuilder);
        }
    }

    public async Task InitializeAsync()
    {
        await postgresSql.StartAsync();

        Services = new ServiceCollection()
            .AddDbContextFactory<AppDbContext>(b =>
            {
                b.UseNpgsql(postgresSql.GetConnectionString());
            })
            .AddAI()
            .AddEntityFrameworkChatStore<AppDbContext>()
            .Services
            .BuildServiceProvider();

        var factory = Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
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
