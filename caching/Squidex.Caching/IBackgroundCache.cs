// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Caching;

public interface IBackgroundCache
{
    Task<T> GetOrCreateAsync<T>(object key, TimeSpan expiration, Func<object, Task<T>> creator, Func<T, Task<bool>>? isValid = null);
}
