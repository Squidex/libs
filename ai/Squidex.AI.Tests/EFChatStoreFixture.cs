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
using Squidex.AI.Implementation;
using Squidex.AI.Mongo;
using Squidex.Hosting;
using Testcontainers.PostgreSql;
using Xunit;

namespace Squidex.AI;

public sealed class EFChatStoreFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgresSql = new PostgreSqlBuilder().Build();
    private IServiceProvider services;

    public EFChatStore<AppDbContext> Store => services.GetRequiredService<EFChatStore<AppDbContext>>();

    public sealed class AppDbContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddChatStore();
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
            .AddEntityFrameworkChatStore<AppDbContext>()
            .BuildServiceProvider();

        foreach (var service in services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.InitializeAsync(default);
        }

        var factory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        var context = await factory.CreateDbContextAsync();
        var creator = (RelationalDatabaseCreator)context.Database.GetService<IDatabaseCreator>();

        await creator.EnsureCreatedAsync();
    }
}
