// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Implementation;

public sealed class DelegateAsyncDisposable : IAsyncDisposable
{
    private readonly Func<ValueTask> action;

    public DelegateAsyncDisposable(Func<ValueTask> action)
    {
        this.action = action;
    }

    public ValueTask DisposeAsync()
    {
        return action();
    }
}
