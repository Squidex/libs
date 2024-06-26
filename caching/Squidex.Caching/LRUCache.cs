﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics.CodeAnalysis;

namespace Squidex.Caching;

public sealed class LRUCache<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, LinkedListNode<LRUCacheItem<TKey, TValue>>> cacheMap = [];
    private readonly LinkedList<LRUCacheItem<TKey, TValue>> cacheHistory = new LinkedList<LRUCacheItem<TKey, TValue>>();
    private readonly int capacity;
    private readonly Action<TKey, TValue> itemEvicted;

    public int Count
    {
        get { return cacheMap.Count; }
    }

    public IEnumerable<TKey> Keys
    {
        get { return cacheMap.Keys; }
    }

    public LRUCache(int capacity, Action<TKey, TValue>? itemEvicted = null)
    {
        if (capacity <= 0)
        {
            throw new ArgumentException("Capacity must be greater than zero.", nameof(capacity));
        }

        this.capacity = capacity;

        this.itemEvicted = itemEvicted ?? ((key, value) => { });
    }

    public void Clear()
    {
        cacheHistory.Clear();
        cacheMap.Clear();
    }

    public bool Set(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (cacheMap.TryGetValue(key, out var node))
        {
            node.Value.Value = value;

            cacheHistory.Remove(node);
            cacheHistory.AddLast(node);

            cacheMap[key] = node;

            return true;
        }

        if (cacheMap.Count >= capacity)
        {
            RemoveFirst();
        }

        var cacheItem = new LRUCacheItem<TKey, TValue> { Key = key, Value = value };

        node = new LinkedListNode<LRUCacheItem<TKey, TValue>>(cacheItem);

        cacheMap.Add(key, node);
        cacheHistory.AddLast(node);

        return false;
    }

    public bool Remove(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (cacheMap.TryGetValue(key, out var node))
        {
            cacheMap.Remove(key);
            cacheHistory.Remove(node);

            return true;
        }

        return false;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        value = default!;

        if (cacheMap.TryGetValue(key, out var node))
        {
            value = node.Value.Value;

            cacheHistory.Remove(node);
            cacheHistory.AddLast(node);

            return true;
        }

        return false;
    }

    public bool Contains(TKey key)
    {
        return cacheMap.ContainsKey(key);
    }

    private void RemoveFirst()
    {
        var node = cacheHistory.First;

        if (node != null)
        {
            itemEvicted(node.Value.Key, node.Value.Value);

            cacheMap.Remove(node.Value.Key);
            cacheHistory.RemoveFirst();
        }
    }
}
