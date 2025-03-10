﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Squidex.AI.Mongo;
using Squidex.Hosting;
using Testcontainers.MongoDb;
using Xunit;

namespace Squidex.AI;

public sealed class MongoChatStoreFixture : IAsyncLifetime
{
    private readonly MongoDbContainer mongoDb =
        new MongoDbBuilder()
            .WithReuse(Debugger.IsAttached)
            .WithLabel("reuse-id", "chatstore-mongo")
            .Build();

    public IServiceProvider Services { get; private set; }

    public MongoChatStore Store => Services.GetRequiredService<MongoChatStore>();

    public async Task InitializeAsync()
    {
        await mongoDb.StartAsync();

        Services = new ServiceCollection()
            .AddSingleton<IMongoClient>(_ => new MongoClient(mongoDb.GetConnectionString()))
            .AddSingleton(c => c.GetRequiredService<IMongoClient>().GetDatabase("Test"))
            .AddAI()
            .AddMongoChatStore(TestHelpers.Configuration)
            .Services
            .BuildServiceProvider();

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

        await mongoDb.StopAsync();
    }
}
