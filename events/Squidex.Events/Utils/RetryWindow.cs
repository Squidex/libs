// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Events.Utils;

public sealed class RetryWindow(TimeSpan windowDuration, int windowSize, TimeProvider? clock = null)
{
    private readonly int windowSize = windowSize + 1;
    private readonly Queue<DateTimeOffset> retries = new Queue<DateTimeOffset>();
    private readonly TimeProvider clock = clock ?? TimeProvider.System;

    public void Reset()
    {
        retries.Clear();
    }

    public bool CanRetryAfterFailure()
    {
        var now = clock.GetUtcNow();

        retries.Enqueue(now);

        while (retries.Count > windowSize)
        {
            retries.Dequeue();
        }

        return retries.Count < windowSize || (retries.Count > 0 && (now - retries.Peek()) > windowDuration);
    }
}
