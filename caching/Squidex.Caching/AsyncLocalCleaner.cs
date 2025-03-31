// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Caching;

internal sealed class AsyncLocalCleaner<T>(AsyncLocal<T> asyncLocal) : IDisposable
{
    public void Dispose()
    {
        asyncLocal.Value = default!;
    }
}
