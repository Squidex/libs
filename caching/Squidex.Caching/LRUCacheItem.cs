// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable SA1401 // Fields must be private

namespace Squidex.Caching;

internal sealed class LRUCacheItem<TKey, TValue>
{
    public TKey Key;

    public TValue Value;
}
