// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Squidex.Hosting;

namespace TestHelpers.MongoDb;

public abstract class MongoFerretFixture(string reuseId = "libs-mongodb") : IAsyncLifetime
{
    public IContainer MongoDb { get; } =
        new ContainerBuilder()
            .WithReuse(Debugger.IsAttached)
            .WithLabel("reuse-id", reuseId)
            .WithImage("ghcr.io/ferretdb/ferretdb-eval:2.4")
            .WithPortBinding(27017, true)
            .WithEnvironment("POSTGRES_USER", "username")
            .WithEnvironment("POSTGRES_PASSWORD", "password")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(27017, o => o.WithTimeout(TimeSpan.FromSeconds(60))))
            .Build();

    public IServiceProvider Services { get; private set; }

    public IMongoClient MongoClient
        => Services.GetRequiredService<IMongoClient>();

    public IMongoDatabase MongoDatabase
        => Services.GetRequiredService<IMongoDatabase>();

    public async Task InitializeAsync()
    {
        await MongoDb.StartAsync();

        var mongoClient = await TryConnectAsync();

        var serviceCollection = new ServiceCollection()
            .AddSingleton<IMongoClient>(mongoClient)
            .AddSingleton(c => c.GetRequiredService<IMongoClient>().GetDatabase("Test"));

        AddServices(serviceCollection);

        Services = serviceCollection.BuildServiceProvider();

        foreach (var service in Services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.InitializeAsync(default);
        }
    }

    private async Task<MongoClient> TryConnectAsync()
    {
        using var cts = new CancellationTokenSource(30_000);
        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                var mongoClient = new MongoClient($"mongodb://username:password@localhost:{MongoDb.GetMappedPublicPort(27017)}/");
                await mongoClient.ListDatabasesAsync(cts.Token);

                return mongoClient;
            }
            catch
            {
                continue;
            }
        }

        var (stdOut, stdError) = await MongoDb.GetLogsAsync(ct: default);

        throw new InvalidOperationException($"Failed connect to ferred DB\n{stdOut}\n{stdError}");
    }

    protected abstract void AddServices(IServiceCollection services);

    public async Task DisposeAsync()
    {
        foreach (var service in Services.GetRequiredService<IEnumerable<IInitializable>>())
        {
            await service.ReleaseAsync(default);
        }

        await MongoDb.StopAsync();
    }
}
