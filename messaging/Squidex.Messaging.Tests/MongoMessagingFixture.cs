// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace Squidex.Messaging;

public sealed class MongoMessagingFixture : IAsyncLifetime
{
    private readonly MongoDbContainer mongoDB = new MongoDbBuilder().Build();

    public IMongoDatabase Database { get; private set; }

    public async Task InitializeAsync()
    {
        await mongoDB.StartAsync();

        var mongoClient = new MongoClient("mongodb://localhost:27017");
        var mongoDatabase = mongoClient.GetDatabase("Messaging_Tests");

        Database = mongoDatabase;
    }

    public async Task DisposeAsync()
    {
        await mongoDB.StopAsync();
    }
}
