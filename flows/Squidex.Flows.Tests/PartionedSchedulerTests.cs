// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Threading.Channels;
using Squidex.Flows.Internal.Execution.Utils;

namespace Squidex.Flows;

public class PartionedSchedulerTests
{
    [Fact]
    public async Task Should_handle_events_in_order_for_same_key()
    {
        var counter = 0;

        var sut = new PartitionedScheduler<int>(async (_, ct) =>
        {
            var x = counter;
            await Task.Delay(1, ct);
            counter = x + 1;
        }, 32, 2);

        for (var i = 0; i < 100; i++)
        {
            await sut.ScheduleAsync(0, i);
        }

        await sut.CompleteAsync();

        Assert.Equal(100, counter);
    }

    [Fact]
    public async Task Should_not_handle_events_in_order_for_different_keys()
    {
        var counter = 0;

        var sut = new PartitionedScheduler<int>(async (_, ct) =>
        {
            var x = counter;
            await Task.Delay(1, ct);
            counter = x + 1;
        }, 32, 2);

        for (var i = 0; i < 500; i++)
        {
            await sut.ScheduleAsync(i, i);
        }

        await sut.CompleteAsync();

        Assert.True(counter < 500);
    }

    [Fact]
    public async Task Should_not_swallow_exception()
    {
        var sut = new PartitionedScheduler<int>((_, ct) =>
        {
            throw new InvalidOperationException();
        }, 32, 2);

        var ex = await Assert.ThrowsAsync<ChannelClosedException>(async () => await ScheduleUntilFailed(sut, 0, 0));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    [Fact]
    public async Task Should_not_swallow_exception_from_other_partition()
    {
        var sut = new PartitionedScheduler<int>((_, ct) =>
        {
            throw new InvalidOperationException();
        }, 32, 2);

        var ex1 = await Assert.ThrowsAsync<ChannelClosedException>(async () => await ScheduleUntilFailed(sut, 1, 1));
        var ex2 = await Assert.ThrowsAsync<ChannelClosedException>(async () => await sut.ScheduleAsync(0, 0));

        Assert.IsType<InvalidOperationException>(ex1.InnerException);
        Assert.IsType<InvalidOperationException>(ex2.InnerException);
    }

    private static async Task ScheduleUntilFailed<T>(PartitionedScheduler<T> sut, object key, T value)
    {
        for (var i = 0; i < 100; i++)
        {
            await sut.ScheduleAsync(key, value);
        }
    }
}
