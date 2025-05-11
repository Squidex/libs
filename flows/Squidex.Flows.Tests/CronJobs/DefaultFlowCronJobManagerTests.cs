// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using Squidex.Flows.CronJobs.Internal;

namespace Squidex.Flows.CronJobs;

public class DefaultFlowCronJobManagerTests
{
    private readonly ICronJobStore<TestFlowContext> cronJobStore = A.Fake<ICronJobStore<TestFlowContext>>();
    private readonly IClock clock = A.Fake<IClock>();
    private readonly Instant now = Instant.FromUtc(2024, 11, 10, 9, 8, 7);
    private readonly DefaultFlowCronJobManager<TestFlowContext> sut;

    public DefaultFlowCronJobManagerTests()
    {
        A.CallTo(() => clock.GetCurrentInstant()).Returns(now);

        sut = new DefaultFlowCronJobManager<TestFlowContext>(
            cronJobStore,
            new NodaCronTimezoneProvider(),
            Options.Create(new CronJobsOptions()),
            A.Fake<ILogger<DefaultFlowCronJobManager<TestFlowContext>>>())
        {
            Clock = clock,
        };
    }

    [Fact]
    public void Should_subscribe()
    {
        sut.Subscribe((job, ct) => Task.CompletedTask);
    }

    [Fact]
    public void Should_subscribe_with_null()
    {
        sut.Subscribe(null!);
    }

    [Fact]
    public async Task Should_forward_deletion_to_store()
    {
        var id = Guid.NewGuid().ToString();

        await sut.RemoveAsync(id);

        A.CallTo(() => cronJobStore.DeleteAsync(id, default))
            .MustHaveHappened();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Europe/Berlin")]
    public async Task Should_add_job_to_store(string? timezone)
    {
        var job = new CronJob<TestFlowContext>
        {
            Id = Guid.NewGuid().ToString(),
            CronExpression = "0 */4 * * *",
            CronTimezone = timezone,
            Context = new TestFlowContext(),
        };

        await sut.AddAsync(job);

        A.CallTo(() => cronJobStore.StoreAsync(
                A<CronJobEntry<TestFlowContext>>.That.Matches(x => x.Job == job),
                A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid")]
    public async Task Should_throw_exception_if_task_has_invalid_cron_expression(string? cronExpression)
    {
        var job = new CronJob<TestFlowContext>
        {
            Id = Guid.NewGuid().ToString(),
            CronExpression = cronExpression!,
            CronTimezone = "Europe/Berlin",
            Context = new TestFlowContext(),
        };

        await Assert.ThrowsAnyAsync<ArgumentException>(() => sut.AddAsync(job));
    }

    [Fact]
    public async Task Should_throw_exception_if_task_has_invalid_timezone()
    {
        var job = new CronJob<TestFlowContext>
        {
            Id = Guid.NewGuid().ToString(),
            CronExpression = "0 */4 * * *",
            CronTimezone = "invalid",
            Context = new TestFlowContext(),
        };

        await Assert.ThrowsAsync<ArgumentException>(() => sut.AddAsync(job));
    }

    [Fact]
    public async Task Should_query_and_update_next_times()
    {
        var result1 =
            new CronJobResult<TestFlowContext>(
                new CronJob<TestFlowContext>
                {
                    Id = Guid.NewGuid().ToString(),
                    CronExpression = "0 */4 * * *",
                    CronTimezone = "invalid",
                    Context = new TestFlowContext(),
                },
                now.Plus(Duration.FromDays(-1)));

        var result2 =
            new CronJobResult<TestFlowContext>(
                new CronJob<TestFlowContext>
                {
                    Id = Guid.NewGuid().ToString(),
                    CronExpression = "0 */4 * * *",
                    CronTimezone = "invalid",
                    Context = new TestFlowContext(),
                },
                now.Plus(Duration.FromDays(1)));

        var result3 =
            new CronJobResult<TestFlowContext>(
                new CronJob<TestFlowContext>
                {
                    Id = Guid.NewGuid().ToString(),
                    CronExpression = "0 */4 * * *",
                    CronTimezone = "invalid",
                    Context = new TestFlowContext(),
                },
                now.Plus(Duration.FromDays(2)));

        var receivedJobs = new List<CronJob<TestFlowContext>>();
        sut.Subscribe((job, ct) =>
        {
            receivedJobs.Add(job);
            return Task.CompletedTask;
        });

        List<CronJobUpdate> updates = [];
        A.CallTo(() => cronJobStore.ScheduleAsync(
                A<List<CronJobUpdate>>._,
                A<CancellationToken>._))
            .Invokes(c => updates = c.GetArgument<List<CronJobUpdate>>(0)!);

        A.CallTo(() => cronJobStore.QueryPendingAsync(A<Instant>._, default))
            .Returns(new List<CronJobResult<TestFlowContext>> { result1, result2, result3 }.ToAsyncEnumerable());

        await sut.UpdateAsync(default);

        updates.Should().BeEquivalentTo(
            [
                new CronJobUpdate(result1.Job.Id, Instant.FromUtc(2024, 11, 10, 12, 0, 0)),
                new CronJobUpdate(result2.Job.Id, Instant.FromUtc(2024, 11, 11, 12, 0, 0)),
                new CronJobUpdate(result3.Job.Id, Instant.FromUtc(2024, 11, 12, 12, 0, 0)),
            ]);

        Assert.Equal(3, receivedJobs.Count);
    }

    [Fact]
    public async Task Should_query_and_update_next_times_without_subscription()
    {
        var result1 =
            new CronJobResult<TestFlowContext>(
                new CronJob<TestFlowContext>
                {
                    Id = Guid.NewGuid().ToString(),
                    CronExpression = "*/5 * * * *",
                    CronTimezone = "invalid",
                    Context = new TestFlowContext(),
                },
                default);

        var result2 =
            new CronJobResult<TestFlowContext>(
                new CronJob<TestFlowContext>
                {
                    Id = Guid.NewGuid().ToString(),
                    CronExpression = "*/5 * * * *",
                    CronTimezone = "invalid",
                    Context = new TestFlowContext(),
                },
                default);

        A.CallTo(() => cronJobStore.QueryPendingAsync(A<Instant>._, default))
            .Returns(new List<CronJobResult<TestFlowContext>> { result1, result2 }.ToAsyncEnumerable());

        await sut.UpdateAsync(default);

        A.CallTo(() => cronJobStore.ScheduleAsync(
                A<List<CronJobUpdate>>.That.Matches(x => x.Count == 2),
                A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Europe/Berlin")]
    public void Should_validate_timezone_successfully(string? input)
    {
        var result = sut.IsValidTimezone(input!);

        Assert.True(result);
    }

    [Theory]
    [InlineData("invalid")]
    public void Should_validate_timezone_when_invalid(string? input)
    {
        var result = sut.IsValidTimezone(input!);

        Assert.False(result);
    }

    [Theory]
    [InlineData("0 */4 * * *")]
    public void Should_validate_expression_successfully(string? input)
    {
        var result = sut.IsValidCronExpression(input!);

        Assert.True(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid")]
    [InlineData("*/5 * * * *")]
    public void Should_validate_expression_when_invalid(string? input)
    {
        var result = sut.IsValidCronExpression(input!);

        Assert.False(result);
    }

    [Fact]
    public void Should_get_available_timezones()
    {
        var timezones = sut.GetAvailableTimezoneIds();

        Assert.NotEmpty(timezones);
    }
}
