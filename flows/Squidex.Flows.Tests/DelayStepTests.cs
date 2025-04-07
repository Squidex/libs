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

    public DelayStepTests()
    {
        A.CallTo(() => clock.GetCurrentInstant()).Returns(now);
    }

    [Fact]
    public async Task Should_delay_call_by_specified_seconds()
    {
        var sut = new DelayStep { Clock = clock, DelayInSec = 10 };

        var result = await sut.ExecuteAsync(null!, default);

        Assert.Equal(now.Plus(Duration.FromSeconds(10)), result.Scheduled);
    }

    [Fact]
    public async Task Should_not_delay_next_call_if_delay_is_negative()
    {
        var sut = new DelayStep { Clock = clock, DelayInSec = -10 };

        var result = await sut.ExecuteAsync(null!, default);

        Assert.Equal(now, result.Scheduled);
    }
}
