// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using NodaTime;
using Squidex.Flows.CronJobs.Internal;

namespace Squidex.Flows.CronJobs;

public abstract class CronJobStoreTests
{
    private readonly Instant now = SystemClock.Instance.GetCurrentInstant();

    protected abstract Task<ICronJobStore<TestFlowContext>> CreateSutAsync();

    [Fact]
    public async Task Should_insert_job()
    {
        var sut = await CreateSutAsync();

        var id = Guid.NewGuid().ToString();
        await sut.StoreAsync(new CronJobEntry<TestFlowContext>
        {
            NextTime = default,
            Job = new CronJob<TestFlowContext>
            {
                Id = id,
                Context = new TestFlowContext { Value = 42 },
                CronExpression = "1/* * * * *",
                CronTimezone = "Europe/Berlin",
            },
        }, default);

        var queried = await sut.QueryPendingAsync(now, default).ToListAsync();

        Assert.Contains(queried, x => x.Job.Id == id && x.Job.Context.Value == 42);
    }

    [Fact]
    public async Task Should_replace_job()
    {
        var sut = await CreateSutAsync();

        var id = Guid.NewGuid().ToString();
        await sut.StoreAsync(new CronJobEntry<TestFlowContext>
        {
            NextTime = default,
            Job = new CronJob<TestFlowContext>
            {
                Id = id,
                Context = new TestFlowContext { Value = 42 },
                CronExpression = "1/* * * * *",
                CronTimezone = "Europe/Berlin",
            },
        }, default);

        await sut.StoreAsync(new CronJobEntry<TestFlowContext>
        {
            NextTime = default,
            Job = new CronJob<TestFlowContext>
            {
                Id = id,
                Context = new TestFlowContext { Value = 44 },
                CronExpression = "1/* * * * *",
                CronTimezone = "Europe/Berlin",
            },
        }, default);

        var queried = await sut.QueryPendingAsync(now, default).ToListAsync();

        Assert.Contains(queried, x => x.Job.Id == id && x.Job.Context.Value == 44);
    }

    [Fact]
    public async Task Should_delete_job()
    {
        var sut = await CreateSutAsync();

        var id = Guid.NewGuid().ToString();
        await sut.StoreAsync(new CronJobEntry<TestFlowContext>
        {
            NextTime = default,
            Job = new CronJob<TestFlowContext>
            {
                Id = id,
                Context = new TestFlowContext { Value = 42 },
                CronExpression = "1/* * * * *",
                CronTimezone = "Europe/Berlin",
            },
        }, default);

        await sut.DeleteAsync(id, default);

        var queried = await sut.QueryPendingAsync(now, default).ToListAsync();

        Assert.DoesNotContain(queried, x => x.Job.Id == id);
    }

    [Fact]
    public async Task Should_only_return_pending_jobs()
    {
        var sut = await CreateSutAsync();

        var id = Guid.NewGuid().ToString();
        await sut.StoreAsync(new CronJobEntry<TestFlowContext>
        {
            NextTime = now.Plus(Duration.FromDays(30)),
            Job = new CronJob<TestFlowContext>
            {
                Id = id,
                Context = new TestFlowContext { Value = 42 },
                CronExpression = "1/* * * * *",
                CronTimezone = "Europe/Berlin",
            },
        }, default);

        var queried1 = await sut.QueryPendingAsync(now, default).ToListAsync();

        Assert.DoesNotContain(queried1, x => x.Job.Id == id);

        await sut.ScheduleAsync(
            [
                new CronJobUpdate(id, now.Minus(Duration.FromDays(30))),
            ], default);

        var queried2 = await sut.QueryPendingAsync(now, default).ToListAsync();

        Assert.Contains(queried2, x => x.Job.Id == id);
    }
}
