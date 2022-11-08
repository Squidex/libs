// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using StackExchange.Redis;
using Xunit;

#pragma warning disable SA1300 // Element should begin with upper-case letter

namespace Squidex.Messaging;

public class RedisMessagingTests : MessagingTestsBase, IClassFixture<RedisFixture>
{
    public RedisFixture _ { get; }

    protected override bool CanHandleAndSimulateTimeout => false;

    public RedisMessagingTests(RedisFixture fixture)
    {
        _ = fixture;
    }

    protected override void ConfigureServices(IServiceCollection services, ChannelName channel, bool consume)
    {
        services
            .AddRedisTransport(TestHelpers.Configuration, options =>
            {
                options.PollingInterval = TimeSpan.FromSeconds(0.1);

                options.ConnectionFactory = log =>
                {
                    return Task.FromResult<IConnectionMultiplexer>(_.Connection);
                };
            })
            .AddMessaging(channel, consume, options =>
            {
                options.Expires = TimeSpan.FromDays(1);
            });
    }
}
