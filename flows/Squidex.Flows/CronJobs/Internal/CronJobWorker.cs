// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Squidex.Hosting;

namespace Squidex.Flows.CronJobs.Internal;

public sealed class CronJobWorker<TContext>(
    ICronJobManager<TContext> cronJobManager,
    IOptions<CronJobsOptions> options,
    ILogger<DefaultCronJobManager<TContext>> log)
    : IBackgroundProcess
{
    private SimpleTimer? timer;

    public Task StartAsync(
        CancellationToken ct)
    {
        timer = new SimpleTimer(cronJobManager.UpdateAllAsync, options.Value.UpdateInterval, log);
        return Task.CompletedTask;
    }

    public async Task StopAsync(
        CancellationToken ct)
    {
        if (timer != null)
        {
            await timer.DisposeAsync();
            timer = null;
        }
    }
}
