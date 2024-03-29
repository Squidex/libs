﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Caching;

public interface ILocalCache
{
    IDisposable StartContext();

    void Add(object key, object? value);

    void Remove(object key);

    bool TryGetValue(object key, out object? value);
}
