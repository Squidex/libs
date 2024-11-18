// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FluentFTP;

namespace Squidex.Assets;

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
#pragma warning disable MA0042 // Do not use blocking calls in an async method
                Return(clientTask.Result.Client);
#pragma warning restore MA0042 // Do not use blocking calls in an async method
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
                var waiting = new TaskCompletionSource<(IAsyncFtpClient, bool)>();

                queue.Enqueue(waiting);

                return waiting.Task;
            }
        }
    }

    public void Return(IAsyncFtpClient client)
    {
        lock (queue)
        {
            if (client.IsDisposed)
            {
                created--;
            }
            else if (queue.TryDequeue(out var waiting))
            {
                waiting.TrySetResult((client, false));
            }
            else
            {
                pool.Enqueue(client);
            }
        }
    }
}
