// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

#pragma warning disable MA0040 // Forward the CancellationToken parameter to methods that take one

namespace Squidex.Messaging;

public class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer redis =
        new RedisBuilder()
            .WithReuse(true)
            .WithLabel("reuse-id", "messaging-redis")
            .Build();

    public ConnectionMultiplexer Connection { get; set; }

    public async Task DisposeAsync()
    {
        await redis.StopAsync();
    }

    public async Task InitializeAsync()
    {
        await redis.StartAsync();

        Connection = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
    }
}
