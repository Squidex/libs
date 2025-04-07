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

            if (!position.ReadFromEnd)
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
            if (position.ReadFromEnd)
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
        var watchStartInSeconds =
            lastPosition.Timestamp.Timestamp > 0 ?
            lastPosition.Timestamp.Timestamp :
            (int)Clock.GetUtcNow().ToUnixTimeSeconds();

        // Start a little bit earlier to get missing events.
        watchStartInSeconds -= 30;

        var changePipeline = Match(streamFilter);
        var changeStart = new BsonTimestamp(watchStartInSeconds, 0);

        // If nothing has been queried, the resume token can be null.
        BsonDocument? resumeToken = null;

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
                    foreach (var storedEvent in change.FullDocument.Filtered(lastPosition))
                    {
                        await eventSubscriber.OnNextAsync(this, storedEvent);
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

        using var cts = new CancellationTokenSource();
        using var ctc = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, stopToken.Token);

        await foreach (var storedEvent in eventStore.QueryAllAsync(streamFilter, position, ct: ctc.Token))
        {
            var queryUntil = Clock.GetUtcNow().UtcDateTime.AddMinutes(-5);

            if (storedEvent.Data.Headers.Timestamp() >= queryUntil)
            {
                // We can actually miss events if we query until events until.
                // because an event with a larger timestamp can be available before an event with a smaller timestamp.
                break;
            }

            await eventSubscriber.OnNextAsync(this, storedEvent);
            lastRawPosition = storedEvent.EventPosition;
        }

        return lastRawPosition;
    }

    private static PipelineDefinition<ChangeStreamDocument<MongoEventCommit>, ChangeStreamDocument<MongoEventCommit>>? Match(StreamFilter streamFilter)
    {
        var filterBuilder = Builders<ChangeStreamDocument<MongoEventCommit>>.Filter;
        var filterResult = filterBuilder.Eq(x => x.OperationType, ChangeStreamOperationType.Insert);

        var byStream = FilterBuilder.ByChangeInStream(streamFilter);
        if (byStream != null)
        {
            filterResult = filterBuilder.And(filterResult, byStream);
        }

        var emptyPipeline = new EmptyPipelineDefinition<ChangeStreamDocument<MongoEventCommit>>();

        return emptyPipeline.Match(filterResult);
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
