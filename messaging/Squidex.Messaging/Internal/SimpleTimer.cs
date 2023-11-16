// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;

#pragma warning disable MA0134 // Observe result of async calls

namespace Squidex.Messaging.Internal;

public sealed class SimpleTimer : IAsyncDisposable
{
    private readonly CancellationTokenSource stopToken = new CancellationTokenSource();

    public bool IsDisposed => stopToken.IsCancellationRequested;

    public SimpleTimer(Func<CancellationToken, Task> action, TimeSpan interval, ILogger log)
    {
        Task.Run(async () =>
        {
            try
            {
                while (!stopToken.IsCancellationRequested)
                {
                    try
                    {
                        await action(stopToken.Token);

                        await Task.Delay(interval, stopToken.Token);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        log.LogWarning(ex, "Failed to execute timer.");
                    }
                }
            }
            catch
            {
                return;
            }
        }, stopToken.Token);
    }

    public async ValueTask DisposeAsync()
    {
        await stopToken.CancelAsync();
    }
}
