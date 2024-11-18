﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using StackExchange.Redis;
using Xunit;

#pragma warning disable SA1300 // Element should begin with upper-case letter

namespace Squidex.Messaging;

public class RedisMessagingTests(RedisFixture fixture) : MessagingTestsBase, IClassFixture<RedisFixture>
{
    public RedisFixture _ { get; } = fixture;

    protected override bool CanHandleAndSimulateTimeout => false;

    protected override void Configure(MessagingBuilder builder)
    {
        builder.AddRedisTransport(TestHelpers.Configuration, options =>
        {
            options.PollingInterval = TimeSpan.FromSeconds(0.1);

            options.ConnectionFactory = log =>
            {
                return Task.FromResult<IConnectionMultiplexer>(_.Connection);
            };
        });
    }
}
