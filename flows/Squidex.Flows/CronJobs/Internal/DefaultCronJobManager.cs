// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
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
    : ICronJobManager<TContext>
{
    private readonly ConcurrentDictionary<string, bool> failedJobs = [];
    private Func<CronJob<TContext>, CancellationToken, Task>? handler;

    public IClock Clock { get; set; } = SystemClock.Instance;

    public async Task UpdateAllAsync(
        CancellationToken ct = default)
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
                if (!TryGetCronExpression(cronJob.CronExpression, false, out var expression))
                {
                    failedJobs.TryAdd(cronJob.Id, true);
                    log.LogWarning("Failed parse expression '{expression}' for id '{id}'",
                        cronJob.Id,
                        cronJob.CronExpression);
                    continue;
                }

                if (!TryGetTimezone(cronJob.CronTimezone, out var timezone))
                {
                    timezone = TimeZoneInfo.Utc;
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
                        false)
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

        if (!TryGetCronExpression(cronJob.CronExpression, true, out var expression))
        {
            throw new ArgumentException("Invalid cron expression.", nameof(cronJob));
        }

        if (!TryGetTimezone(cronJob.CronTimezone, out var timezone))
        {
            throw new ArgumentException("Invalid timezone.", nameof(cronJob));
        }

        var now = Clock.GetCurrentInstant().ToDateTimeOffset();
        var next =
            expression.GetOccurrences(
                now,
                now.AddYears(1),
                timezone,
                false)
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

    public IReadOnlyList<string> GetAvailableTimezoneIds()
    {
        return cronTimezones.GetAvailableIds();
    }

    public bool IsValidCronExpression(string expression)
    {
        return TryGetCronExpression(expression, true, out var _);
    }

    public bool IsValidTimezone(string? timezone)
    {
        return TryGetTimezone(timezone, out var _);
    }

    private bool TryGetTimezone(string? id, [MaybeNullWhen(false)] out TimeZoneInfo result)
    {
        result = TimeZoneInfo.Utc;
        if (string.IsNullOrWhiteSpace(id))
        {
            return true;
        }

        return cronTimezones.TryParse(id, out result);
    }

    private bool TryGetCronExpression(string expression, bool validateInterval, [MaybeNullWhen(false)] out CronExpression result)
    {
        result = null!;
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        if (!CronExpression.TryParse(expression, out var parsed))
        {
            return false;
        }

        if (validateInterval)
        {
            var now = DateTime.UtcNow;
            var occurrences =
                parsed.GetOccurrences(
                    now,
                    now.AddYears(1),
                    true)
                .Take(100)
                .ToList();

            for (var i = 0; i < occurrences.Count - 1; i++)
            {
                var timeBetween = occurrences[i + 1] - occurrences[i];
                if (timeBetween < options.Value.MinimumInterval)
                {
                    return false;
                }
            }
        }

        result = parsed;
        return true;
    }
}
