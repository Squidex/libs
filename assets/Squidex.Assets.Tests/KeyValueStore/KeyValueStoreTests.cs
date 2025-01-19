// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Assets.KeyValueStore;

public abstract class KeyValueStoreTests
{
    protected abstract Task<IAssetKeyValueStore<TestValue>> CreateSutAsync();

    [Fact]
    public async Task Should_insert_and_fetch_value()
    {
        var sut = await CreateSutAsync();

        var key = Guid.NewGuid().ToString();
        await sut.SetAsync(key, new TestValue { Value = key }, DateTimeOffset.Now.AddHours(1));

        var queried = await sut.GetAsync(key);

        Assert.Equal(key, queried?.Value);
    }

    [Fact]
    public async Task Should_update_and_fetch_value()
    {
        var sut = await CreateSutAsync();

        var key = Guid.NewGuid().ToString();
        var value0 = $"{key}_v0";
        var value1 = $"{key}_v1";

        await sut.SetAsync(key, new TestValue { Value = value0 }, DateTimeOffset.UtcNow.AddHours(1));
        await sut.SetAsync(key, new TestValue { Value = value1 }, DateTimeOffset.UtcNow.AddHours(1));

        var queried = await sut.GetAsync(key);

        Assert.Equal(value1, queried?.Value);
    }

    [Fact]
    public async Task Should_delete_entity()
    {
        var sut = await CreateSutAsync();

        var key = Guid.NewGuid().ToString();
        await sut.SetAsync(key, new TestValue { Value = key }, DateTimeOffset.UtcNow.AddHours(1));
        await sut.DeleteAsync(key);

        var queried = await sut.GetAsync(key);

        Assert.Null(queried);
    }

    [Fact]
    public async Task Should_query_expired_items()
    {
        var sut = await CreateSutAsync();

        var key1 = Guid.NewGuid().ToString();
        var key2 = Guid.NewGuid().ToString();
        var key3 = Guid.NewGuid().ToString();

        await sut.SetAsync(key1, new TestValue { Value = key1 }, DateTimeOffset.UtcNow.AddHours(1));
        await sut.SetAsync(key2, new TestValue { Value = key2 }, DateTimeOffset.UtcNow.AddHours(-3));
        await sut.SetAsync(key3, new TestValue { Value = key3 }, DateTimeOffset.UtcNow.AddHours(1));

        var query = sut.GetExpiredEntriesAsync(DateTimeOffset.UtcNow);
        var expired = new List<string>();

        await foreach (var (key, _) in query)
        {
            expired.Add(key);
        }

        Assert.Contains(key2, expired);
        Assert.DoesNotContain(key1, expired);
        Assert.DoesNotContain(key3, expired);
    }
}
