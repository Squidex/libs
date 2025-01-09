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
    : BackgroundProcess, IMessagingDataStore where T : DbContext
{
    protected override async Task ExecuteAsync(
        CancellationToken ct)
    {
        if (options.Value.CleanupTime <= TimeSpan.Zero)
        {
            log.LogInformation("Skipping cleanup, because cleanup time is less or equal than zero.");
            return;
        }

        var timer = new PeriodicTimer(options.Value.CleanupTime);
        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                await CleanupAsync(ct);
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

    public async Task<IReadOnlyList<Entry>> GetEntriesAsync(string group,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group);

        await using var context = await dbContextFactory.CreateDbContextAsync(ct);

        var result = new List<Entry>();

        var queryTime = timeProvider.GetUtcNow().UtcDateTime;
        var queryFilter = context.Set<EFMessagingDataEntity>().Where(x => x.Group == group && x.Expiration < queryTime);

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
        foreach (var entry in entries)
        {
            var entity = new EFMessagingDataEntity
            {
                Expiration = entry.Expiration,
                Key = entry.Group,
                ValueData = entry.Value.Data,
                ValueFormat = entry.Value.Format,
                ValueType = entry.Value.TypeString,
                Group = entry.Group,
            };

            await context.Set<EFMessagingDataEntity>().AddAsync(entity, ct);
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string group, string key,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        await using var context = await dbContextFactory.CreateDbContextAsync(ct);

        await context.Set<EFMessagingDataEntity>().Where(x => x.Group == group && x.Key == key).ExecuteDeleteAsync(ct);
    }

    public async Task CleanupAsync(
        CancellationToken ct)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(ct);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        await context.Set<EFMessagingDataEntity>().Where(x => x.Expiration < now).ExecuteDeleteAsync(ct);
    }
}
