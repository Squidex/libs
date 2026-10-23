// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Squidex.Caching;

public class BackgroundCacheTests
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(20);
    private readonly Random random = new Random();
    private readonly BackgroundCache sut;
    private int factoryInvoked;
    private DateTimeOffset now = DateTimeOffset.UtcNow;

    public BackgroundCacheTests()
    {
        sut = new BackgroundCache(CreateMemoryCache(), CreateTimeProvider(), Options.Create(new BackgroundCacheOptions
        {
            RetryDelay = RetryDelay,
        }), NullLogger<BackgroundCache>.Instance);
    }

    [Fact]
    public async Task Should_get_from_factory_if_not_found()
    {
        var value1 = await GetOrCreateValue();
        var value2 = await GetOrCreateValue();

        Assert.Equal(11, value1);
        Assert.Equal(11, value2);
        Assert.Equal(1, factoryInvoked);
    }

    [Fact]
    public async Task Should_recreate_in_background_if_invalid()
    {
        var value1 = await GetOrCreateValue();
        var value2 = await GetOrCreateValue(isValid: false);

        await Task.Delay(500);

        var value3 = await GetOrCreateValue();

        Assert.Equal(11, value1);
        Assert.Equal(11, value2);
        Assert.Equal(12, value3);
        Assert.Equal(2, factoryInvoked);
    }

    [Fact]
    public async Task Should_get_from_factory_if_not_found_and_create_once()
    {
        var values = await Task.WhenAll(Enumerable.Repeat(1, 20).Select(x => GetOrCreateValue()));

        foreach (var value in values)
        {
            Assert.Equal(11, value);
        }

        Assert.Equal(1, factoryInvoked);
    }

    [Fact]
    public async Task Should_recreate_in_background()
    {
        var initialValues = await Task.WhenAll(Enumerable.Repeat(1, 20).Select(x => GetOrCreateValue()));

        foreach (var value in initialValues)
        {
            Assert.Equal(11, value);
        }

        now += TimeSpan.FromMinutes(58);

        var valuesAfterExpiration = await Task.WhenAll(Enumerable.Repeat(1, 20).Select(x => GetOrCreateValue()));

        foreach (var value in valuesAfterExpiration)
        {
            Assert.Equal(11, value);
        }

        await Task.Delay(500);

        var valuesAfterRecreation = await Task.WhenAll(Enumerable.Repeat(1, 20).Select(x => GetOrCreateValue()));

        foreach (var value in valuesAfterRecreation)
        {
            Assert.Equal(12, value);
        }

        Assert.Equal(2, factoryInvoked);
    }

    [Fact]
    public async Task Should_cache_failed_creation_only_until_retry_delay()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => GetOrCreateFailingValue(fail: true));
        await Assert.ThrowsAsync<InvalidOperationException>(() => GetOrCreateFailingValue(fail: false));

        // Longer than the default delay, so the configured delay must be used.
        now += RetryDelay - TimeSpan.FromSeconds(1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => GetOrCreateFailingValue(fail: false));

        now += TimeSpan.FromSeconds(2);

        var value = await GetOrCreateFailingValue(fail: false);

        Assert.Equal(42, value);
        Assert.Equal(2, factoryInvoked);
    }

    [Fact]
    public async Task Should_cache_canceled_creation_only_until_retry_delay()
    {
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            sut.GetOrCreateAsync<int>("BackgroundKey", TimeSpan.FromHours(1), x => Task.FromCanceled<int>(new CancellationToken(true))));
        await Assert.ThrowsAsync<TaskCanceledException>(() => GetOrCreateFailingValue(fail: false));

        now += RetryDelay + TimeSpan.FromSeconds(1);

        var value = await GetOrCreateFailingValue(fail: false);

        Assert.Equal(42, value);
        Assert.Equal(1, factoryInvoked);
    }

    [Fact]
    public async Task Should_keep_previous_value_if_refresh_fails()
    {
        var value1 = await GetOrCreateValue();

        await RefreshAndFail();

        var value2 = await GetOrCreateValue();

        Assert.Equal(11, value1);
        Assert.Equal(11, value2);
    }

    [Fact]
    public async Task Should_retry_failed_refresh_only_after_retry_delay()
    {
        await GetOrCreateValue();
        await RefreshAndFail();

        var valueBeforeRetry = await GetOrCreateValue(isValid: false);

        await Task.Delay(200);

        Assert.Equal(1, factoryInvoked);

        now += RetryDelay + TimeSpan.FromSeconds(1);

        await GetOrCreateValue(isValid: false);
        await Task.Delay(500);

        var valueAfterRetry = await GetOrCreateValue();

        Assert.Equal(11, valueBeforeRetry);
        Assert.Equal(12, valueAfterRetry);
        Assert.Equal(2, factoryInvoked);
    }

    private async Task RefreshAndFail()
    {
        var refreshed = new TaskCompletionSource();

        await sut.GetOrCreateAsync<int>("BackgroundKey", TimeSpan.FromHours(1), x =>
        {
            refreshed.SetResult();
            throw new InvalidOperationException();
        }, x => Task.FromResult(false));

        await refreshed.Task;

        // Wait until the failure has been handled.
        await Task.Delay(100);
    }

    [Fact]
    public async Task Should_not_flow_async_local_state_into_refresh()
    {
        var asyncLocal = new AsyncLocal<string?>();
        var asyncLocalInValidator = new TaskCompletionSource<string?>();
        var asyncLocalInCreator = new TaskCompletionSource<string?>();

        await GetOrCreateValue();

        asyncLocal.Value = "Request";

        await sut.GetOrCreateAsync("BackgroundKey", TimeSpan.FromHours(1), x =>
        {
            asyncLocalInCreator.SetResult(asyncLocal.Value);
            return Task.FromResult(12);
        }, x =>
        {
            asyncLocalInValidator.SetResult(asyncLocal.Value);
            return Task.FromResult(false);
        });

        Assert.Null(await asyncLocalInValidator.Task);
        Assert.Null(await asyncLocalInCreator.Task);
        Assert.Equal("Request", asyncLocal.Value);
    }

    private Task<int> GetOrCreateFailingValue(bool fail)
    {
        return sut.GetOrCreateAsync("BackgroundKey", TimeSpan.FromHours(1), async x =>
        {
            Interlocked.Increment(ref factoryInvoked);

            await Task.Delay(10);

            if (fail)
            {
                throw new InvalidOperationException();
            }

            return 42;
        });
    }

    private Task<int> GetOrCreateValue(bool isValid = true)
    {
        return sut.GetOrCreateAsync("BackgroundKey", TimeSpan.FromHours(1), async x =>
        {
            Interlocked.Increment(ref factoryInvoked);

            await Task.Delay(random.Next(50 + 100));

            return factoryInvoked + 10;
        }, x => Task.FromResult(isValid));
    }

    private TimeProvider CreateTimeProvider()
    {
        var timeProvider = A.Fake<TimeProvider>();

        A.CallTo(() => timeProvider.GetUtcNow())
            .ReturnsLazily(() => now);

        return timeProvider;
    }

    private static MemoryCache CreateMemoryCache()
    {
        return new MemoryCache(Options.Create(new MemoryCacheOptions()));
    }
}
