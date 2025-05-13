// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.CompilerServices;
using System.Text.Json;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Extensions;
using Squidex.Flows.CronJobs;
using Squidex.Flows.CronJobs.Internal;

namespace Squidex.Flows.EntityFramework;

public sealed class EFCronJobStore<TDbContext, TContext>(
    IDbContextFactory<TDbContext> dbContextFactory,
    JsonSerializerOptions jsonSerializerOptions)
    : ICronJobStore<TContext>
    where TDbContext : DbContext
{
    public async IAsyncEnumerable<CronJobResult<TContext>> QueryPendingAsync(Instant now,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var queryLimit = now.ToDateTimeOffset();
        var queryItems = dbContext.Set<EFCronJobEntity>().Where(x => x.DueTime < queryLimit).AsAsyncEnumerable();

        await foreach (var item in queryItems.WithCancellation(ct))
        {
            yield return new CronJobResult<TContext>(
                JsonSerializer.Deserialize<CronJob<TContext>>(item.Data, jsonSerializerOptions)!,
                item.DueTime.ToInstant());
        }
    }

    public async Task ScheduleAsync(List<CronJobUpdate> updates,
        CancellationToken ct)
    {
        if (updates.Count == 0)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entities =
            updates.Select(x =>
                new EFCronJobEntity
                {
                    Id = x.Id,
                    DueTime = x.NextTime.ToDateTimeOffset(),
                    Data = null!,
                });

        await dbContext.BulkUpdateAsync(
            entities,
            new BulkConfig { PropertiesToIncludeOnUpdate = [nameof(EFCronJobEntity.DueTime)] },
            cancellationToken: ct);
    }

    public async Task StoreAsync(CronJobEntry<TContext> entry,
        CancellationToken ct)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = new EFCronJobEntity
        {
            Id = entry.Job.Id,
            Data = JsonSerializer.Serialize(entry.Job, jsonSerializerOptions),
            DueTime = entry.NextTime.ToDateTimeOffset(),
        };

        await dbContext.Set<EFCronJobEntity>()
            .Where(x => x.Id == entry.Job.Id)
            .ExecuteDeleteAsync(ct);

        await dbContext.Set<EFCronJobEntity>().AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string id,
        CancellationToken ct)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        await dbContext.Set<EFCronJobEntity>()
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(ct);
    }
}
