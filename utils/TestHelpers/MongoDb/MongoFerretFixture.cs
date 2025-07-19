// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
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
            .WithImage("ghcr.io/ferretdb/ferretdb-eval")
            .WithPortBinding(27017, true)
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new WaitIndicateReadiness()))
            .WithEnvironment("POSTGRES_USER", "username")
            .WithEnvironment("POSTGRES_PASSWORD", "password")
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

    private sealed class WaitIndicateReadiness : IWaitUntil
    {
        private static readonly string[] LineEndings = ["\r\n", "\n"];

        public async Task<bool> UntilAsync(IContainer container)
        {
            var (stdout, stderr) = await container.GetLogsAsync(since: container.StoppedTime, timestampsEnabled: false)
                .ConfigureAwait(false);

            var waitingLogs =
                Array.Empty<string>()
                    .Concat(stdout.Split(LineEndings, StringSplitOptions.RemoveEmptyEntries))
                    .Concat(stderr.Split(LineEndings, StringSplitOptions.RemoveEmptyEntries))
                    .Count(line => line.Contains("database system is ready to accept connections", StringComparison.Ordinal));

            return waitingLogs > 0;
        }
    }
}
