// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Squidex.Hosting;

namespace Squidex.Messaging.EntityFramework;

internal class EFMessagingDataStore<T>(
    IDbContextFactory<T> dbContextFactory,
    IOptions<EFMessagingDataStoreOptions> options,
    TimeProvider timeProvider,
    ILogger<EFMessagingDataStore<T>> log)
    : IMessagingDataStore, IBackgroundProcess where T : DbContext
{
    private SimpleTimer? timer;

    public Task StartAsync(
        CancellationToken ct)
    {
        timer = new SimpleTimer(CleanupAsync, options.Value.CleanupTime, log);
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

    public async Task<IReadOnlyList<Entry>> GetEntriesAsync(string group,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group);

        await using var context = await dbContextFactory.CreateDbContextAsync(ct);

        var result = new List<Entry>();

        var queryTime = timeProvider.GetUtcNow().UtcDateTime;
        var queryFilter = context.Set<EFMessagingDataEntity>().Where(x => x.Group == group && x.Expiration > queryTime);

        await foreach (var entity in queryFilter.AsAsyncEnumerable().WithCancellation(ct))
        {
            var value = new SerializedObject(entity.ValueData, entity.ValueType, entity.ValueFormat);

            result.Add(new Entry(group, entity.Key, value, entity.Expiration));
        }

        return result;
    }

    public async Task StoreManyAsync(Entry[] entries,
        CancellationToken ct)
    {
        if (entries.Length == 0)
        {
            return;
        }

        await using var context = await dbContextFactory.CreateDbContextAsync(ct);
        foreach (var (group, key, value, expiration) in entries)
        {
            try
            {
                var entity = new EFMessagingDataEntity
                {
                    Expiration = expiration,
                    Key = key,
                    ValueData = value.Data,
                    ValueFormat = value.Format,
                    ValueType = value.TypeString,
                    Group = group,
                };

                try
                {
                    await context.Set<EFMessagingDataEntity>().AddAsync(entity, ct);
                    await context.SaveChangesAsync(ct);
                }
                finally
                {
                    context.Entry(entity).State = EntityState.Detached;
                }
            }
            catch (DbUpdateException)
            {
                await context.Set<EFMessagingDataEntity>()
                    .Where(x => x.Group == group && x.Key == key)
                    .ExecuteUpdateAsync(b => b
                        .SetProperty(x => x.Expiration, expiration)
                        .SetProperty(x => x.ValueData, value.Data)
                        .SetProperty(x => x.ValueFormat, value.Format)
                        .SetProperty(x => x.ValueType, value.TypeString),
                        ct);
            }
        }
    }

    public async Task DeleteAsync(string group, string key,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        await using var context = await dbContextFactory.CreateDbContextAsync(ct);

        await context.Set<EFMessagingDataEntity>().Where(x => x.Group == group && x.Key == key)
            .ExecuteDeleteAsync(ct);
    }

    public async Task CleanupAsync(
        CancellationToken ct)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(ct);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        await context.Set<EFMessagingDataEntity>().Where(x => x.Expiration < now)
            .ExecuteDeleteAsync(ct);
    }
}
