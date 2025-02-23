// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Squidex.Assets.EntityFramework;

public sealed class EFAssetKeyValueStore<TContext, TEntity>(
    IDbContextFactory<TContext> dbContextFactory,
    JsonSerializerOptions jsonSerializerOptions)
    : IAssetKeyValueStore<TEntity>
    where TContext : DbContext
    where TEntity : class
{
    public async Task DeleteAsync(string key,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        await dbContext.Set<EFAssetKeyValueEntity<TEntity>>().Where(x => x.Key == key)
            .ExecuteDeleteAsync(ct);
    }

    public async Task<TEntity?> GetAsync(string key,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.Set<EFAssetKeyValueEntity<TEntity>>().Where(x => x.Key == key)
            .FirstOrDefaultAsync(ct);

        if (entity == null)
        {
            return default;
        }

        return entity?.GetValue(jsonSerializerOptions);
    }

    public async IAsyncEnumerable<(string Key, TEntity Value)> GetExpiredEntriesAsync(DateTimeOffset now,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var query = dbContext.Set<EFAssetKeyValueEntity<TEntity>>().Where(x => x.Expires < now);

        foreach (var entity in query)
        {
            yield return (entity.Key, entity.GetValue(jsonSerializerOptions));
        }
    }

    public async Task SetAsync(string key, TEntity value, DateTimeOffset expiration,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = EFAssetKeyValueEntity<TEntity>.Create(key, value, expiration, jsonSerializerOptions);
        try
        {
            await dbContext.Set<EFAssetKeyValueEntity<TEntity>>().AddAsync(entity, ct);
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(entity).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();
        }
    }
}
