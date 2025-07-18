﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Threading.Channels;

namespace Squidex.Flows.Internal.Execution.Utils;

public sealed class PartitionedScheduler<T> : IAsyncDisposable
{
    private readonly Consumer[] consumers;

    private sealed class Consumer
    {
        private readonly Channel<T> channel;
        private readonly Task worker;

        public Consumer(Func<T, CancellationToken, Task> action, int bufferSize,
            CancellationToken ct)
        {
            channel = Channel.CreateBounded<T>(new BoundedChannelOptions(bufferSize)
            {
                SingleReader = true,
                SingleWriter = false,
            });

            worker = Task.Run(async () =>
            {
                try
                {
                    await foreach (var item in channel.Reader.ReadAllAsync(ct))
                    {
                        await action(item, ct);
                    }
                }
                catch (Exception ex)
                {
                    var flatten = Flatten(ex);

                    channel.Writer.Complete(flatten);
                }
            }, ct);
        }

        public ValueTask ScheduleAsync(T item,
            CancellationToken ct)
        {
            return channel.Writer.WriteAsync(item, ct);
        }

        public void TryFail(Exception exception)
        {
            channel.Writer.TryComplete(exception);
        }

        public Task CompleteAsync()
        {
            channel.Writer.TryComplete();
            return worker;
        }

        internal static Exception Flatten(Exception ex)
        {
            while (ex is ChannelClosedException closed && closed.InnerException is ChannelClosedException)
            {
                ex = closed.InnerException;
            }

            return ex;
        }
    }

    public PartitionedScheduler(Func<T, CancellationToken, Task> action,
        int maxWorkers,
        int maxBuffer,
        CancellationToken ct = default)
    {
        consumers = new Consumer[maxWorkers];

        for (var i = 0; i < maxWorkers; i++)
        {
            consumers[i] = new Consumer(action, maxBuffer, ct);
        }
    }

    public async ValueTask ScheduleAsync(object key, T item,
        CancellationToken ct = default)
    {
        try
        {
            var consumerIndex = Math.Abs((key?.GetHashCode() ?? 0) % consumers.Length);
            var consumerInstance = consumers[consumerIndex];

            await consumerInstance.ScheduleAsync(item, ct);
        }
        catch (Exception ex)
        {
            var flatten = Consumer.Flatten(ex);

            foreach (var consumer in consumers)
            {
                consumer.TryFail(flatten);
            }

            throw flatten;
        }
    }

    public async Task CompleteAsync()
    {
        foreach (var consumer in consumers)
        {
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
            try
            {
                await consumer.CompleteAsync();
            }
            catch
            {
                // Ensure we can complete all workers.
            }
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CompleteAsync();
    }
}
