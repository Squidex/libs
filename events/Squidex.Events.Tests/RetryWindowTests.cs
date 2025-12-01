// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Events.Utils;

namespace Squidex.Events;

public class RetryWindowTests
{
    private readonly TimeProvider clock = A.Fake<TimeProvider>();
    private DateTimeOffset now = DateTimeOffset.UtcNow;

    public RetryWindowTests()
    {
        A.CallTo(() => clock.GetUtcNow())
            .ReturnsLazily(() => now);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_handle_non_positive_window_size_as_rate_limiter(int windowSize)
    {
        var sut = new RetryWindow(TimeSpan.FromSeconds(10), windowSize, clock);

        Assert.True(sut.CanRetryAfterFailure());
        now = now.AddSeconds(1);
        Assert.False(sut.CanRetryAfterFailure());
        now = now.AddSeconds(11);
        Assert.True(sut.CanRetryAfterFailure());
    }

    [Fact]
    public void Should_allow_retries_within_window_size()
    {
        var sut = new RetryWindow(TimeSpan.FromMinutes(1), 3, clock);

        Assert.True(sut.CanRetryAfterFailure());
        Assert.True(sut.CanRetryAfterFailure());
        Assert.True(sut.CanRetryAfterFailure());

        Assert.False(sut.CanRetryAfterFailure());
    }

    [Fact]
    public void Should_allow_retry_after_window_duration_expires()
    {
        var sut = new RetryWindow(TimeSpan.FromMinutes(1), 3, clock);

        Assert.True(sut.CanRetryAfterFailure());
        Assert.True(sut.CanRetryAfterFailure());
        Assert.True(sut.CanRetryAfterFailure());
        Assert.False(sut.CanRetryAfterFailure());

        now = now.AddMinutes(2);

        Assert.True(sut.CanRetryAfterFailure());
    }

    [Fact]
    public void Should_slide_window_as_time_passes()
    {
        var sut = new RetryWindow(TimeSpan.FromSeconds(30), 2, clock);

        Assert.True(sut.CanRetryAfterFailure());

        now = now.AddSeconds(5);
        Assert.True(sut.CanRetryAfterFailure());

        now = now.AddSeconds(5);
        Assert.False(sut.CanRetryAfterFailure());

        now = now.AddSeconds(26);
        Assert.True(sut.CanRetryAfterFailure());
    }

    [Fact]
    public void Should_reset_window()
    {
        var sut = new RetryWindow(TimeSpan.FromMinutes(1), 2, clock);

        Assert.True(sut.CanRetryAfterFailure());
        Assert.True(sut.CanRetryAfterFailure());
        Assert.False(sut.CanRetryAfterFailure());

        sut.Reset();

        Assert.True(sut.CanRetryAfterFailure());
        Assert.True(sut.CanRetryAfterFailure());
    }

    [Fact]
    public void Should_handle_windowSize_one()
    {
        var sut = new RetryWindow(TimeSpan.FromSeconds(10), 1, clock);

        Assert.True(sut.CanRetryAfterFailure());
        Assert.False(sut.CanRetryAfterFailure());
        Assert.False(sut.CanRetryAfterFailure());

        now = now.AddSeconds(11);

        Assert.True(sut.CanRetryAfterFailure());
    }

    [Fact]
    public void Should_maintain_queue_size_correctly()
    {
        var sut = new RetryWindow(TimeSpan.FromMinutes(5), 3, clock);

        for (int i = 0; i < 10; i++)
        {
            sut.CanRetryAfterFailure();
            now = now.AddSeconds(1);
        }

        now = now.AddMinutes(6);
        Assert.True(sut.CanRetryAfterFailure());
    }
}
