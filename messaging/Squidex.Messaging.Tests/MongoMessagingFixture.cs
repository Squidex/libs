// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace Squidex.Messaging;

public sealed class MongoMessagingFixture : IAsyncLifetime
{
    private readonly MongoDbContainer mongoDb =
        new MongoDbBuilder()
            .WithReuse(Debugger.IsAttached)
            .WithLabel("reuse-id", "messaging-mongo")
            .Build();

    public IMongoDatabase Database { get; private set; }

    public async Task InitializeAsync()
    {
        await mongoDb.StartAsync();

        var mongoClient = new MongoClient(mongoDb.GetConnectionString());
        var mongoDatabase = mongoClient.GetDatabase("Messaging_Tests");

        Database = mongoDatabase;
    }

    public async Task DisposeAsync()
    {
        await mongoDb.StopAsync();
    }
}
