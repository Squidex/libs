﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body

namespace Squidex.Assets;

internal sealed class AsyncLock
{
    private readonly SemaphoreSlim semaphore;

    public AsyncLock()
    {
        semaphore = new SemaphoreSlim(1);
    }

    public Task<IDisposable> LockAsync()
    {
        var wait = semaphore.WaitAsync();

        if (wait.IsCompleted)
        {
            return Task.FromResult((IDisposable)new LockReleaser(this));
        }
        else
        {
            return wait.ContinueWith(x => (IDisposable)new LockReleaser(this),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    private sealed class LockReleaser : IDisposable
    {
        private AsyncLock? target;

        internal LockReleaser(AsyncLock target)
        {
            this.target = target;
        }

        public void Dispose()
        {
            var current = target;

            if (current == null)
            {
                return;
            }

            target = null;

            try
            {
                current.semaphore.Release();
            }
            catch
            {
                // just ignore the Exception
            }
        }
    }
}
