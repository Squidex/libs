// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Hosting;

public abstract class BackgroundProcess : IBackgroundProcess
{
    private Task executeTask;
    private CancellationTokenSource? stoppingToken;

    public virtual string Name => GetType().Name;

    public virtual int Order => 0;

    public Task StartAsync(
        CancellationToken ct)
    {
        stoppingToken = CancellationTokenSource.CreateLinkedTokenSource(ct);

        executeTask = ExecuteAsync(stoppingToken.Token);
        // If the task is completed then return it, this will bubble cancellation and failure to the caller
        if (executeTask.IsCompleted)
        {
            return executeTask;
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(
        CancellationToken ct)
    {
        if (executeTask == null || stoppingToken == null)
        {
            return;
        }

        try
        {
#pragma warning disable MA0042 // Do not use blocking calls in an async method
            stoppingToken.Cancel();
#pragma warning restore MA0042 // Do not use blocking calls in an async method
        }
        finally
        {
            // Wait until the task completes or the stop token triggers
            await Task.WhenAny(executeTask, Task.Delay(Timeout.Infinite, ct)).ConfigureAwait(false);
        }
    }

    protected abstract Task ExecuteAsync(
        CancellationToken ct);
}
