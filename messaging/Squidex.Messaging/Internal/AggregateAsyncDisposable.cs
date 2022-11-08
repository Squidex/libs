// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Internal;

public sealed class AggregateAsyncDisposable : IAsyncDisposable
{
    private readonly IAsyncDisposable[] inners;

    public AggregateAsyncDisposable(params IAsyncDisposable[] inners)
    {
        this.inners = inners;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var inner in inners)
        {
            await inner.DisposeAsync();
        }
    }
}
