// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squidex.Hosting;
using Testcontainers.MariaDb;

namespace TestHelpers.EntityFramework;

public abstract class MariaDbFixture<TContext>(string? reuseId = null) : IAsyncLifetime where TContext : DbContext
{
    public MariaDbContainer MariaDb { get; } =
        new MariaDbBuilder()
            .WithReuse(true)
            .WithLabel("reuse-id", reuseId)
            .WithCommand("--log-bin-trust-function-creators=1", "--local-infile=1")
            .Build();

    public IServiceProvider Services { get; private set; }

    public IDbContextFactory<TContext> DbContextFactory
        => Services.GetRequiredService<IDbContextFactory<TContext>>();

    public async Task InitializeAsync()
    {
        await MariaDb.StartAsync();

        var connectionString = $"{MariaDb.GetConnectionString()};AllowLoadLocalInfile=true;MaxPoolSize=1000";

        var serviceCollection =
            new ServiceCollection()
                .AddSingleton<IInitializable, DbContextInitializer<TContext>>()
                .AddDbContextFactory<TContext>(b =>
                {
                    b.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
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

        await MariaDb.StopAsync();
    }
}
