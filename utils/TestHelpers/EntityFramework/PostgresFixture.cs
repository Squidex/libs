// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squidex.Hosting;
using Testcontainers.PostgreSql;

namespace TestHelpers.EntityFramework;

public abstract class PostgresFixture<TContext>(string? reuseId = null) : IAsyncLifetime where TContext : DbContext
{
    public PostgreSqlContainer PostgreSql { get; } =
        new PostgreSqlBuilder()
            .WithImage("postgis/postgis")
            .WithReuse(true)
            .WithLabel("reuse-id", reuseId)
            .Build();

    public IServiceProvider Services { get; private set; }

    public IDbContextFactory<TContext> DbContextFactory
        => Services.GetRequiredService<IDbContextFactory<TContext>>();

    public async Task InitializeAsync()
    {
        await PostgreSql.StartAsync();

        var serviceCollection =
            new ServiceCollection()
                .AddSingleton<IInitializable, DbContextInitializer<TContext>>()
                .AddDbContextFactory<TContext>(b =>
                {
                    b.UseNpgsql(PostgreSql.GetConnectionString());
                });

        AddServices(serviceCollection);

        Services = serviceCollection.BuildServiceProvider();

        foreach (var service in Services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.InitializeAsync(default);
        }
    }

    protected abstract void AddServices(IServiceCollection services);

    public async Task DisposeAsync()
    {
        foreach (var service in Services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.ReleaseAsync(default);
        }

        await PostgreSql.StopAsync();
    }
}
