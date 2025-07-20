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
            .WithPortBinding(8088, true)
            .WithEnvironment("POSTGRES_USER", "username")
            .WithEnvironment("POSTGRES_PASSWORD", "password")
            .WithWaitStrategy(
                 Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(
                     x => x.ForPath("/debug/readyz").ForPort(8088),
                     x => x.WithTimeout(TimeSpan.FromSeconds(60))))
            .Build();

    public IServiceProvider Services { get; private set; }

    public IMongoClient MongoClient
        => Services.GetRequiredService<IMongoClient>();

    public IMongoDatabase MongoDatabase
        => Services.GetRequiredService<IMongoDatabase>();

    public async Task InitializeAsync()
    {
        await MongoDb.StartAsync();

        var serviceCollection = new ServiceCollection()
            .AddSingleton<IMongoClient>(_ => new MongoClient($"mongodb://username:password@localhost:{MongoDb.GetMappedPublicPort(27017)}/"))
            .AddSingleton(c => c.GetRequiredService<IMongoClient>().GetDatabase("Test"));

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

        await MongoDb.StopAsync();
    }
}
