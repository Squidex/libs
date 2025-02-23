// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;

namespace Squidex.Hosting;

public sealed class SimpleTimer : IAsyncDisposable
{
    private readonly CancellationTokenSource stopToken = new CancellationTokenSource();
    private readonly Task task;

    public bool IsDisposed => stopToken.IsCancellationRequested;

    public SimpleTimer(Func<CancellationToken, Task> action, TimeSpan interval, ILogger? log)
    {
        if (interval <= TimeSpan.Zero)
        {
            return;
        }

        task = Task.Run(async () =>
        {
            try
            {
                var timer = new PeriodicTimer(interval);
                while (await timer.WaitForNextTickAsync(stopToken.Token))
                {
                    try
                    {
                        await action(stopToken.Token);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        log?.LogWarning(ex, "Failed to execute timer.");
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
        try
        {
            await task;
        }
        catch
        {
            return;
        }
    }
}
