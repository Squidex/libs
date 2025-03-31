// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Implementation;

public sealed class DelegateAsyncDisposable(Func<ValueTask> action) : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        return action();
    }
}
