// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using Cronos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Extensions;
using Squidex.Hosting;

namespace Squidex.Flows.CronJobs.Internal;

public class DefaultCronJobManager<TContext>(
    ICronJobStore<TContext> cronJobStore,
    ICronTimezoneProvider cronTimezones,
    IOptions<CronJobsOptions> options,
    ILogger<DefaultCronJobManager<TContext>> log)
    : ICronJobManager<TContext>, IBackgroundProcess
{
    private readonly ConcurrentDictionary<string, bool> failedJobs = [];
    private Func<CronJob<TContext>, CancellationToken, Task>? handler;
    private SimpleTimer? timer;

    public IClock Clock { get; set; } = SystemClock.Instance;

    public Task StartAsync(
        CancellationToken ct)
    {
        timer = new SimpleTimer(UpdateAsync, options.Value.UpdateInterval, log);
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

    public async Task UpdateAsync(
        CancellationToken ct)
    {
        var currentHandler = handler;
        var currentUpdates = new List<CronJobUpdate>();

        var nowInstant = Clock.GetCurrentInstant();
        var nowDateTime = nowInstant.ToDateTimeOffset();

        // This is the lower limit. If the last time is too old, we use the current date, so that not
        // Invoke the cron job dozens of times after a server downtime.
        var lowerLimit = nowDateTime - options.Value.UpdateLimit;

        await foreach (var (cronJob, lastTime) in cronJobStore.QueryPendingAsync(nowInstant, ct))
        {
            if (failedJobs.ContainsKey(cronJob.Id))
            {
                continue;
            }

            try
            {
                if (currentHandler != null)
                {
                    await currentHandler(cronJob, ct);
                }

                // Ignore expressions, which cannot be parsed anymore.
                if (!CronExpression.TryParse(cronJob.CronExpression, out var expression))
                {
                    failedJobs.TryAdd(cronJob.Id, true);
                    log.LogWarning("Failed parse expression '{expression}' for id '{id}'",
                        cronJob.Id,
                        cronJob.CronExpression);

                    try
                    {
                        await cronJobStore.DeleteAsync(cronJob.Id, ct);
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Failed to delete cron job with id '{id}'", cronJob.Id);
                    }
                }

                var timezone = TimeZoneInfo.Utc;
                // Just ignore timezones that we cannot parse anymore.
                if (!string.IsNullOrWhiteSpace(cronJob.CronTimezone) && cronTimezones.TryParse(cronJob.CronTimezone, out var zone))
                {
                    timezone = zone;
                }

                var lastDateTimeOffset = lastTime.ToDateTimeOffset();
                if (lastDateTimeOffset < lowerLimit)
                {
                    lastDateTimeOffset = nowDateTime;
                }

                var next =
                    expression.GetOccurrences(
                        lastDateTimeOffset,
                        lastDateTimeOffset.AddYears(1),
                        timezone,
                        fromInclusive: false)
                    .FirstOrDefault();

                if (next == default)
                {
                    log.LogWarning("Failed to get next occurrency for cron job '{id}' and expression '{expression}'",
                        cronJob.Id,
                        cronJob.CronExpression);
                    continue;
                }

                currentUpdates.Add(new CronJobUpdate(cronJob.Id, next.ToInstant()));
            }
            catch (Exception ex)
            {
                failedJobs.TryAdd(cronJob.Id, true);
                log.LogError(ex, "Failed to handle cron job with id '{id}'", cronJob.Id);
            }
        }

        if (currentUpdates.Count <= 0)
        {
            return;
        }

        try
        {
            await cronJobStore.ScheduleAsync(currentUpdates, ct);
        }
        catch (Exception ex)
        {
            log.LogCritical(ex, "Failed to reschedule cron jobs.");
            foreach (var (id, _) in currentUpdates)
            {
                failedJobs.TryAdd(id, true);
            }
        }
    }

    public void Subscribe(Func<CronJob<TContext>, CancellationToken, Task> handler)
    {
        this.handler = handler;
    }

    public async Task AddAsync(CronJob<TContext> cronJob,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cronJob);

        if (!CronExpression.TryParse(cronJob.CronExpression, out var expression))
        {
            throw new ArgumentException("Invalid cron expression.", nameof(cronJob));
        }

        var timezone = TimeZoneInfo.Utc;
        if (!string.IsNullOrWhiteSpace(cronJob.CronTimezone) && !cronTimezones.TryParse(cronJob.CronTimezone, out timezone))
        {
            throw new ArgumentException("Invalid timezone.", nameof(cronJob));
        }

        var now = Clock.GetCurrentInstant().ToDateTimeOffset();
        var next =
            expression.GetOccurrences(
                now,
                now.AddYears(1),
                timezone,
                fromInclusive: false)
            .FirstOrDefault();

        if (next == default)
        {
            throw new ArgumentException("Invalid cron expression.", nameof(cronJob));
        }

        failedJobs.TryRemove(cronJob.Id, out var _);
        await cronJobStore.StoreAsync(new CronJobEntry<TContext> { NextTime = next.ToInstant(), Job = cronJob }, ct);
    }

    public Task RemoveAsync(string id,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        return cronJobStore.DeleteAsync(id, ct);
    }
}
