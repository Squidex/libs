﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FluentFTP;
using Squidex.Assets.Internal;

namespace Squidex.Assets
{
    internal sealed class FTPClientPool
    {
        private readonly Queue<TaskCompletionSource<(IFtpClient, bool)>> queue = new Queue<TaskCompletionSource<(IFtpClient, bool)>>();
        private readonly Queue<IFtpClient> pool = new Queue<IFtpClient>();
        private readonly Func<IFtpClient> clientFactory;
        private readonly int clientsLimit;
        private int created;

        public FTPClientPool(Func<IFtpClient> clientFactory, int clientsLimit)
        {
            Guard.NotNull(clientFactory, nameof(clientFactory));

            this.clientFactory = clientFactory;
            this.clientsLimit = clientsLimit;
        }

        public async Task<(IFtpClient, bool IsNew)> GetClientAsync(
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

        private Task<(IFtpClient Client, bool IsNew)> GetClientCoreAsync()
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
                    var waiting = new TaskCompletionSource<(IFtpClient, bool)>();

                    queue.Enqueue(waiting);

                    return waiting.Task;
                }
            }
        }

        public void Return(IFtpClient client)
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
}
