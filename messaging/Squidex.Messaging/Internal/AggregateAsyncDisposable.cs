// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Internal;

public sealed class AggregateAsyncDisposable(params IAsyncDisposable[] inners) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        foreach (var inner in inners)
        {
            await inner.DisposeAsync();
        }
    }
}
