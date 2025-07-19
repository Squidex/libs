// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Squidex.Hosting;
using TestHelpers;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Events;

public sealed class MongoEventStoreFerretDbFixture : IAsyncLifetime
{
    public IServiceProvider Services { get; private set; }

    public IMongoClient MongoClient
        => Services.GetRequiredService<IMongoClient>();

    public IMongoDatabase MongoDatabase
        => Services.GetRequiredService<IMongoDatabase>();

    public async Task InitializeAsync()
    {
        var serviceCollection = new ServiceCollection()
            .AddSingleton<IMongoClient>(_ => new MongoClient("mongodb://username:password@localhost:27018/"))
            .AddSingleton(c => c.GetRequiredService<IMongoClient>().GetDatabase("Test"));

        serviceCollection.AddMongoEventStore(TestUtils.Configuration, options =>
        {
            options.PollingInterval = TimeSpan.FromSeconds(0.1);
        });

        Services = serviceCollection.BuildServiceProvider();

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
    }
}

[Trait("Category", "Dependencies")]
public class MongoEventStoreFerretDbTests(MongoEventStoreFerretDbFixture fixture)
    : EventStoreTests, IClassFixture<MongoEventStoreFerretDbFixture>
{
    protected override Task<IEventStore> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<IEventStore>();
        return Task.FromResult(store);
    }
}
