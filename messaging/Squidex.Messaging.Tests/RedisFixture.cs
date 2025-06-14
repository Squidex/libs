﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics;
using StackExchange.Redis;
using Testcontainers.Redis;

#pragma warning disable MA0040 // Forward the CancellationToken parameter to methods that take one

namespace Squidex.Messaging;

public class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer redis =
        new RedisBuilder()
            .WithReuse(Debugger.IsAttached)
            .WithLabel("reuse-id", "messaging-redis")
            .Build();

    public ConnectionMultiplexer Connection { get; set; }

    public async Task InitializeAsync()
    {
        await redis.StartAsync();

        Connection = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        await redis.StopAsync();
    }
}
