// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Squidex.Hosting;
using TestHelpers;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Events;

public sealed class MongoEventStoreDocumentDbFixture : IAsyncLifetime
{
    public IServiceProvider Services { get; private set; }

    public IMongoClient MongoClient
        => Services.GetRequiredService<IMongoClient>();

    public IMongoDatabase MongoDatabase
        => Services.GetRequiredService<IMongoDatabase>();

    public async Task InitializeAsync()
    {
        var settings = MongoClientSettings.FromConnectionString(
            TestUtils.Configuration.GetValue<string>("documentDb:configuration")
        );

        var certPath = TestUtils.Configuration.GetValue<string>("documentDb:keyFile")!;
        var certFile = new X509Certificate2(certPath);

        settings.RetryWrites = false;
        settings.RetryReads = false;
        settings.SslSettings = new SslSettings
        {
            ClientCertificates = [certFile],
            CheckCertificateRevocation = false,
            ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true,
        };

        var serviceCollection = new ServiceCollection()
            .AddSingleton<IMongoClient>(_ => new MongoClient(settings))
            .AddSingleton(c => c.GetRequiredService<IMongoClient>().GetDatabase("Test"))
            .AddMongoEventStore(TestUtils.Configuration, options =>
            {
                options.PollingInterval = TimeSpan.FromSeconds(0.1);
                options.UseChangeStreams = true;
                options.Derivate = Mongo.MongoDerivate.DocumentDB;
            }).Services;

        Services = serviceCollection.BuildServiceProvider();

        await EnableChangeStreamsAsync();
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

    private async Task EnableChangeStreamsAsync()
    {
        var adminDb = MongoClient.GetDatabase("admin");

        var command = new BsonDocument
        {
            { "modifyChangeStreams", 1 },
            { "database", "Test" },
            { "collection", string.Empty },
            { "enable", true },
        };

        await adminDb.RunCommandAsync<BsonDocument>(command);
    }
}

[Trait("Category", "Dependencies")]
public class MongoEventStoreDocumentDbTests(MongoEventStoreDocumentDbFixture fixture)
    : EventStoreTests, IClassFixture<MongoEventStoreDocumentDbFixture>
{
    protected override Task<IEventStore> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<IEventStore>();
        return Task.FromResult(store);
    }
}
