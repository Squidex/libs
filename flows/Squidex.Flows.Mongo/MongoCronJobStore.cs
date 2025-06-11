// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.CompilerServices;
using System.Text.Json;
using MongoDB.Driver;
using NodaTime;
using NodaTime.Extensions;
using Squidex.Flows.CronJobs;
using Squidex.Flows.CronJobs.Internal;
using Squidex.Hosting;

namespace Squidex.Flows.Mongo;

public sealed class MongoCronJobStore<TContext>(IMongoDatabase database, JsonSerializerOptions jsonSerializerOptions)
    : ICronJobStore<TContext>, IInitializable
{
    private readonly IMongoCollection<MongoCronJobEntity> collection = database.GetCollection<MongoCronJobEntity>("CronJobs");

    public async Task InitializeAsync(
        CancellationToken ct)
    {
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<MongoCronJobEntity>(
                Builders<MongoCronJobEntity>.IndexKeys
                    .Ascending(x => x.DueTime)),
            cancellationToken: ct);
    }

    public async IAsyncEnumerable<CronJobResult<TContext>> QueryPendingAsync(Instant now,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var queryLimit = now.ToDateTimeOffset();
        var queryItems = await collection.Find(x => x.DueTime < queryLimit).ToCursorAsync(ct);

        while (await queryItems.MoveNextAsync(ct))
        {
            foreach (var item in queryItems.Current)
            {
                yield return new CronJobResult<TContext>(
                    JsonSerializer.Deserialize<CronJob<TContext>>(item.Data, jsonSerializerOptions)!,
                    item.DueTime.ToInstant());
            }
        }
    }

    public async Task ScheduleAsync(List<CronJobUpdate> updates,
        CancellationToken ct)
    {
        if (updates.Count == 0)
        {
            return;
        }

        var batch = new List<UpdateOneModel<MongoCronJobEntity>>();
        foreach (var update in updates)
        {
            batch.Add(
                new UpdateOneModel<MongoCronJobEntity>(
                    Builders<MongoCronJobEntity>.Filter.Eq(x => x.Id, update.Id),
                    Builders<MongoCronJobEntity>.Update.Set(x => x.DueTime, update.NextTime.ToDateTimeOffset())));
        }

        await collection.BulkWriteAsync(batch, cancellationToken: ct);
    }

    public async Task StoreAsync(CronJobEntry<TContext> entry,
        CancellationToken ct)
    {
        await collection.ReplaceOneAsync(x => x.Id == entry.Job.Id,
            new MongoCronJobEntity
            {
                Id = entry.Job.Id,
                Data = JsonSerializer.Serialize(entry.Job, jsonSerializerOptions),
                DueTime = entry.NextTime.ToDateTimeOffset(),
            },
            new ReplaceOptions { IsUpsert = true },
            ct);
    }

    public Task DeleteAsync(string id,
        CancellationToken ct)
    {
        return collection.DeleteManyAsync(x => x.Id == id, ct);
    }
}
