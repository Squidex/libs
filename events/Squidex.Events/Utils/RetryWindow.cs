// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Events.Utils;

public sealed class RetryWindow(TimeSpan windowDuration, int windowSize, TimeProvider? clock = null)
{
    private readonly int windowToKeep = windowSize + 1;
    private readonly Queue<DateTimeOffset> retries = new Queue<DateTimeOffset>();
    private readonly TimeProvider clock = clock ?? TimeProvider.System;

    public void Reset()
    {
        retries.Clear();
    }

    public bool CanRetryAfterFailure()
    {
        var now = clock.GetUtcNow();

        if (windowSize <= 0)
        {
            // First attempt is always allowed
            if (retries.Count == 0)
            {
                retries.Enqueue(now);
                return true;
            }

            var last = retries.Dequeue();
            retries.Enqueue(now);
            return (now - last) > windowDuration;
        }

        retries.Enqueue(now);
        while (retries.Count > windowToKeep)
        {
            retries.Dequeue();
        }

        // Allow retry if:
        // 1. Haven't reached the window size limit yet, OR
        // 2. The oldest retry in the queue is older than windowDuration (window has "expired")
        return retries.Count < windowToKeep || (retries.Count > 0 && (now - retries.Peek()) > windowDuration);
    }
}
