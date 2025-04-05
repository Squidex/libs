// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using NodaTime;
using Squidex.Flows.Internal.Execution;

namespace Squidex.Flows;

public class DefaultRetryErrorPolicyTests
{
    private readonly Instant now = SystemClock.Instance.GetCurrentInstant();
    private readonly DefaultRetryErrorPolicy<TestFlowContext> sut = new DefaultRetryErrorPolicy<TestFlowContext>();


    [Fact]
    public void Should_not_retry_for_no_attempts()
    {
        var stepState = new ExecutionStepState
        {
            Attempts = [],
        };

        var next = sut.ShouldRetry(null!, stepState, new NoopStep(), now);

        Assert.Null(next);
    }

    [Fact]
    public void Should_not_retry_if_disabled_for_step()
    {
        var stepState = new ExecutionStepState
        {
            Attempts =
            [
                new ExecutionStepAttempt(),
            ],
        };

        var next = sut.ShouldRetry(null!, stepState, new NotRetryableNoopStep(), now);

        Assert.Null(next);
    }

    [Fact]
    public void Should_retry_after_1_attempts()
    {
        var stepState = new ExecutionStepState
        {
            Attempts =
            [
                new ExecutionStepAttempt(),
            ],
        };

        var next = sut.ShouldRetry(null!, stepState, new NoopStep(), now);

        Assert.Equal(now.Plus(Duration.FromMinutes(5)), next);
    }

    [Fact]
    public void Should_retry_after_2_attempts()
    {
        var stepState = new ExecutionStepState
        {
            Attempts =
            [
                new ExecutionStepAttempt(),
                new ExecutionStepAttempt(),
            ],
        };

        var next = sut.ShouldRetry(null!, stepState, new NoopStep(), now);

        Assert.Equal(now.Plus(Duration.FromHours(1)), next);
    }

    [Fact]
    public void Should_retry_after_3_attempts()
    {
        var stepState = new ExecutionStepState
        {
            Attempts =
            [
                new ExecutionStepAttempt(),
                new ExecutionStepAttempt(),
                new ExecutionStepAttempt(),
            ],
        };

        var next = sut.ShouldRetry(null!, stepState, new NoopStep(), now);

        Assert.Equal(now.Plus(Duration.FromHours(6)), next);
    }

    [Fact]
    public void Should_retry_after_4_attempts()
    {
        var stepState = new ExecutionStepState
        {
            Attempts =
            [
                new ExecutionStepAttempt(),
                new ExecutionStepAttempt(),
                new ExecutionStepAttempt(),
                new ExecutionStepAttempt(),
            ],
        };

        var next = sut.ShouldRetry(null!, stepState, new NoopStep(), now);

        Assert.Equal(now.Plus(Duration.FromHours(12)), next);
    }

    [Fact]
    public void Should_retry_after_5_attempts()
    {
        var stepState = new ExecutionStepState
        {
            Attempts =
            [
                new ExecutionStepAttempt(),
                new ExecutionStepAttempt(),
                new ExecutionStepAttempt(),
                new ExecutionStepAttempt(),
                new ExecutionStepAttempt(),
            ],
        };

        var next = sut.ShouldRetry(null!, stepState, new NoopStep(), now);

        Assert.Null(next);
    }
}
