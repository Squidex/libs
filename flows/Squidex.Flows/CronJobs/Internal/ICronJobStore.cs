// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

using NodaTime;

namespace Squidex.Flows.CronJobs.Internal;

public interface ICronJobStore<TContext>
{
    IAsyncEnumerable<CronJobResult<TContext>> QueryPendingAsync(Instant now,
        CancellationToken ct);

    Task StoreAsync(CronJobEntry<TContext> task,
        CancellationToken ct);

    Task ScheduleAsync(List<CronJobUpdate> updates,
        CancellationToken ct);

    Task DeleteAsync(string id,
        CancellationToken ct);
}

public readonly record struct CronJobUpdate(string Id, Instant NextTime);

public readonly record struct CronJobResult<TContext>(CronJob<TContext> Job, Instant LastTime);
