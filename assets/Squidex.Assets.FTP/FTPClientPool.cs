// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FluentFTP;

namespace Squidex.Assets.FTP;

internal sealed class FTPClientPool(Func<IAsyncFtpClient> clientFactory, int clientsLimit)
{
    private readonly Queue<TaskCompletionSource<(IAsyncFtpClient, bool)>> queue = new Queue<TaskCompletionSource<(IAsyncFtpClient, bool)>>();
    private readonly Queue<IAsyncFtpClient> pool = new Queue<IAsyncFtpClient>();
    private int created;

    public async Task<(IAsyncFtpClient, bool IsNew)> GetClientAsync(
        CancellationToken ct)
    {
        var clientTask = GetClientCoreAsync();

        try
        {
            return await clientTask.WaitAsync(ct);
        }
        catch
        {
            if (clientTask.Status == TaskStatus.RanToCompletion)
            {
                // The task has already completed, so this does not block.
                Return(clientTask.GetAwaiter().GetResult().Client);
            }

            throw;
        }
    }

    private Task<(IAsyncFtpClient Client, bool IsNew)> GetClientCoreAsync()
    {
        lock (queue)
        {
            if (pool.TryDequeue(out var client))
            {
                return Task.FromResult((client, false));
            }

            if (created < clientsLimit)
            {
                var newClient = clientFactory();

                created++;

                return Task.FromResult((newClient, true));
            }
            else
            {
                // Run continuations asynchronously, otherwise the waiter resumes inline on the thread
                // that returns the client, while that thread still holds the lock below.
                var waiting = new TaskCompletionSource<(IAsyncFtpClient, bool)>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                queue.Enqueue(waiting);

                return waiting.Task;
            }
        }
    }

    public void Return(IAsyncFtpClient client)
    {
        TaskCompletionSource<(IAsyncFtpClient, bool)>? waiting = null;

        lock (queue)
        {
            if (client.IsDisposed)
            {
                created--;
            }
            else if (!queue.TryDequeue(out waiting))
            {
                pool.Enqueue(client);
            }
        }

        // Complete the waiter outside of the lock, so that no continuation can run while it is held.
        waiting?.TrySetResult((client, false));
    }
}
