// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using NodaTime;
using Squidex.Flows.Steps;

namespace Squidex.Flows;

public class DelayStepTests
{
    private readonly IClock clock = A.Fake<IClock>();
    private readonly Instant now = SystemClock.Instance.GetCurrentInstant();
    private readonly FlowExecutionContext executionContext;

    public DelayStepTests()
    {
        A.CallTo(() => clock.GetCurrentInstant()).Returns(now);

        executionContext = new FlowExecutionContext(null!, null!, null!, null!, (_, _) => { }, false);
    }

    [Fact]
    public async Task Should_delay_call_by_specified_seconds()
    {
        var sut = new DelayFlowStep { DelayInSec = 10 }.SetClock(clock);

        var result = await sut.ExecuteAsync(executionContext, default);

        Assert.Equal(now.Plus(Duration.FromSeconds(10)), result.Scheduled);
    }

    [Fact]
    public async Task Should_not_delay_next_call_if_delay_is_negative()
    {
        var sut = new DelayFlowStep { DelayInSec = -10 };

        var result = await sut.ExecuteAsync(executionContext, default);

        Assert.Equal(default, result.Scheduled);
    }

    [Fact]
    public async Task Should_not_delay_next_call_if_delay_is_larger_than_one_day()
    {
        var sut = new DelayFlowStep { DelayInSec = (int)TimeSpan.FromDays(1.1).TotalSeconds }.SetClock(clock);

        var result = await sut.ExecuteAsync(executionContext, default);

        Assert.Equal(default, result.Scheduled);
    }
}
