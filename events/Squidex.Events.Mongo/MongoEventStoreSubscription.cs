// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Bson;
using MongoDB.Driver;
using Squidex.Events.Utils;

namespace Squidex.Events.Mongo;

public sealed class MongoEventStoreSubscription : IEventSubscription
{
    private readonly MongoEventStore eventStore;
    private readonly IEventSubscriber<StoredEvent> eventSubscriber;
    private readonly CancellationTokenSource stopToken = new CancellationTokenSource();

    public TimeProvider Clock { get; set; } = TimeProvider.System;

    public MongoEventStoreSubscription(MongoEventStore eventStore, IEventSubscriber<StoredEvent> eventSubscriber, StreamFilter streamFilter, StreamPosition position)
    {
        this.eventStore = eventStore;
        this.eventSubscriber = eventSubscriber;

        QueryAsync(streamFilter, position).Forget();
    }

    private async Task QueryAsync(StreamFilter streamFilter, StreamPosition position)
    {
        try
        {
            StreamPosition lastRawPosition = default;

            if (position.IsEnd)
            {
                try
                {
                    lastRawPosition = await QueryOldAsync(streamFilter, position);
                }
                catch (OperationCanceledException)
                {
                }
            }

            if (stopToken.IsCancellationRequested)
            {
                return;
            }

            ParsedStreamPosition parsedPosition;
            if (lastRawPosition.IsEnd)
            {
                parsedPosition = Clock.GetUtcNow();
            }
            else
            {
                parsedPosition = lastRawPosition;
            }

            await QueryCurrentAsync(streamFilter, parsedPosition);
        }
        catch (Exception ex)
        {
            await eventSubscriber.OnErrorAsync(this, ex);
        }
    }

    private async Task QueryCurrentAsync(StreamFilter streamFilter, ParsedStreamPosition lastPosition)
    {
        BsonDocument? resumeToken = null;

        var start =
            lastPosition.Timestamp.Timestamp > 0 ?
            lastPosition.Timestamp.Timestamp - 30 :
            Clock.GetUtcNow().Add(-TimeSpan.FromSeconds(30)).ToUnixTimeSeconds();

        var changePipeline = Match(streamFilter);
        var changeStart = new BsonTimestamp((int)start, 0);

        while (!stopToken.IsCancellationRequested)
        {
            var changeOptions = new ChangeStreamOptions();

            if (resumeToken != null)
            {
                changeOptions.StartAfter = resumeToken;
            }
            else
            {
                changeOptions.StartAtOperationTime = changeStart;
            }

            using (var cursor = eventStore.TypedCollection.Watch(changePipeline, changeOptions, stopToken.Token))
            {
                var isRead = false;
                await cursor.ForEachAsync(async change =>
                {
                    if (change.OperationType == ChangeStreamOperationType.Insert)
                    {
                        foreach (var storedEvent in change.FullDocument.Filtered(lastPosition))
                        {
                            await eventSubscriber.OnNextAsync(this, storedEvent);
                        }
                    }

                    isRead = true;
                }, stopToken.Token);

                resumeToken = cursor.GetResumeToken();

                if (!isRead)
                {
                    await Task.Delay(1000, stopToken.Token);
                }
            }
        }
    }

    private async Task<StreamPosition> QueryOldAsync(StreamFilter streamFilter, string? position)
    {
        string? lastRawPosition = null;

        using (var cts = new CancellationTokenSource())
        {
            using (var combined = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, stopToken.Token))
            {
                await foreach (var storedEvent in eventStore.QueryAllAsync(streamFilter, position, ct: combined.Token))
                {
                    var now = Clock.GetUtcNow();

                    var timeToNow = now - new DateTimeOffset(storedEvent.Data.Headers.Timestamp(), default);
                    if (timeToNow <= TimeSpan.FromMinutes(5))
                    {
                        await cts.CancelAsync();
                    }
                    else
                    {
                        await eventSubscriber.OnNextAsync(this, storedEvent);
                        lastRawPosition = storedEvent.EventPosition;
                    }
                }
            }
        }

        return lastRawPosition;
    }

    private static PipelineDefinition<ChangeStreamDocument<MongoEventCommit>, ChangeStreamDocument<MongoEventCommit>>? Match(StreamFilter streamFilter)
    {
        var result = new EmptyPipelineDefinition<ChangeStreamDocument<MongoEventCommit>>();

        var byStream = FilterBuilder.ByChangeInStream(streamFilter);

        if (byStream != null)
        {
            var filterBuilder = Builders<ChangeStreamDocument<MongoEventCommit>>.Filter;
            var filterExpression = filterBuilder.Or(filterBuilder.Ne(x => x.OperationType, ChangeStreamOperationType.Insert), byStream);

            return result.Match(filterExpression);
        }

        return result;
    }

    public void Dispose()
    {
        stopToken.Cancel();
    }

    public ValueTask CompleteAsync()
    {
        return default;
    }

    public void WakeUp()
    {
    }
}
