// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Squidex.Hosting;
using StackExchange.Redis;

namespace Squidex.Messaging.Redis;

internal sealed class RedisQueueSubscription : IAsyncDisposable, IMessageAck
{
    private readonly SimpleTimer timer;

    public RedisQueueSubscription(string topicName, IDatabase database, MessageTransportCallback callback,
        TimeSpan pollingInterval, ILogger log)
    {
        timer = new SimpleTimer(async ct =>
        {
            while (true)
            {
                var popped = await database.ListRightPopAsync(topicName);

                if (!popped.HasValue)
                {
                    break;
                }

                var deserialized = JsonSerializer.Deserialize<TransportMessage>(popped.ToString())!;

                await callback(new TransportResult(deserialized, null), this, ct);
            }
        }, pollingInterval, log);
    }

    public ValueTask DisposeAsync()
    {
        return timer.DisposeAsync();
    }

    Task IMessageAck.OnErrorAsync(TransportResult result,
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    Task IMessageAck.OnSuccessAsync(TransportResult result,
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
