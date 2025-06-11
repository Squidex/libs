// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squidex.Hosting;
using Testcontainers.MsSql;

namespace TestHelpers.EntityFramework;

public abstract class SqlServerFixture<TContext>(string? reuseId = null) : IAsyncLifetime where TContext : DbContext
{
    public MsSqlContainer SqlServer { get; } =
        new MsSqlBuilder()
            .WithImage("vibs2006/sql_server_fts")
            .WithReuse(true)
            .WithLabel("reuse-id", reuseId)
            .Build();

    public IServiceProvider Services { get; private set; }

    public IDbContextFactory<TContext> DbContextFactory
        => Services.GetRequiredService<IDbContextFactory<TContext>>();

    public async Task InitializeAsync()
    {
        await SqlServer.StartAsync();
        await SqlServer.ExecScriptAsync($"create database squidex;");

        var serviceCollection =
            new ServiceCollection()
                .AddSingleton<IInitializable, DbContextInitializer<TContext>>()
                .AddDbContextFactory<TContext>(b =>
                {
                    b.UseSqlServer(GetConnectionString());
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

        await SqlServer.StopAsync();
    }

    private string GetConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(SqlServer.GetConnectionString())
        {
            InitialCatalog = "squidex",
        };

        return builder.ConnectionString;
    }
}
