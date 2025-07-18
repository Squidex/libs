﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;

namespace Squidex.Caching;

public class LRUCacheTests
{
    private readonly LRUCache<string, int> sut = new LRUCache<string, int>(10);
    private readonly string key = "Key";

    [Fact]
    public void Should_always_override_when_setting_value()
    {
        sut.Set(key, 1);
        sut.Set(key, 2);

        Assert.True(sut.TryGetValue(key, out var value));
        Assert.True(sut.Contains(key));

        Assert.Equal(2, value);
    }

    [Fact]
    public void Should_clear_items()
    {
        sut.Set("1", 1);
        sut.Set("2", 2);

        Assert.Equal(2, sut.Count);

        sut.Clear();

        Assert.Equal(0, sut.Count);
    }

    [Fact]
    public void Should_remove_old_items_when_capacity_reached()
    {
        for (var i = 0; i < 15; i++)
        {
            sut.Set(i.ToString(CultureInfo.InvariantCulture), i);
        }

        for (var i = 0; i < 5; i++)
        {
            Assert.False(sut.TryGetValue(i.ToString(CultureInfo.InvariantCulture), out var value));
            Assert.Equal(0, value);
        }

        for (var i = 5; i < 15; i++)
        {
            Assert.True(sut.TryGetValue(i.ToString(CultureInfo.InvariantCulture), out var value));
            Assert.Equal(i, value);
        }
    }

    [Fact]
    public void Should_notify_about_evicted_items()
    {
        var evicted = new List<int>();

        var cache = new LRUCache<int, int>(3, (key, _) => evicted.Add(key));

        cache.Set(1, 1);
        cache.Set(2, 2);
        cache.Set(3, 3);
        cache.Set(1, 1);
        cache.Set(4, 4);
        cache.Set(5, 5);

        Assert.Equal([2, 3], evicted);
    }

    [Fact]
    public void Should_return_false_when_item_to_remove_does_not_exist()
    {
        Assert.False(sut.Remove(key));
    }

    [Fact]
    public void Should_remove_inserted_item()
    {
        sut.Set(key, 2);

        Assert.True(sut.Remove(key));
        Assert.False(sut.Contains(key));
        Assert.False(sut.TryGetValue(key, out var value));
        Assert.Equal(0, value);
    }
}
