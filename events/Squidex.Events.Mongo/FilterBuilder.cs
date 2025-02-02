// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.RegularExpressions;
using MongoDB.Driver;

namespace Squidex.Events.Mongo;

internal static class FilterBuilder
{
    public static FilterDefinition<MongoEventCommit> ByOffset(long streamPosition)
    {
        var builder = Builders<MongoEventCommit>.Filter;

        return builder.Gte(x => x.EventStreamOffset, streamPosition);
    }

    public static FilterDefinition<MongoEventCommit> ByPosition(ParsedStreamPosition streamPosition)
    {
        var builder = Builders<MongoEventCommit>.Filter;

        if (streamPosition.IsEndOfCommit)
        {
            return builder.Gt(x => x.Timestamp, streamPosition.Timestamp);
        }
        else
        {
            return builder.Gte(x => x.Timestamp, streamPosition.Timestamp);
        }
    }

    public static FilterDefinition<MongoEventCommit> ByStream(StreamFilter filter)
    {
        var builder = Builders<MongoEventCommit>.Filter;

        if (filter.Prefixes == null)
        {
            return builder.Exists(x => x.EventStream, true);
        }

        if (filter.Kind == StreamFilterKind.MatchStart)
        {
            return builder.Or(filter.Prefixes.Select(p => Buildregex(p, builder)));
        }

        return builder.In(x => x.EventStream, filter.Prefixes);
    }

    private static FilterDefinition<MongoEventCommit> Buildregex(string prefix, FilterDefinitionBuilder<MongoEventCommit> builder)
    {
        if (prefix.StartsWith('%'))
        {
            prefix = $"([a-zA-Z0-9]+){prefix[1..]}";
        }

        return builder.Regex(x => x.EventStream, $"^{prefix}");
    }

    public static FilterDefinition<ChangeStreamDocument<MongoEventCommit>>? ByChangeInStream(StreamFilter filter)
    {
        var builder = Builders<ChangeStreamDocument<MongoEventCommit>>.Filter;

        if (filter.Prefixes == null)
        {
            return null;
        }

        if (filter.Kind == StreamFilterKind.MatchStart)
        {
            return builder.Or(filter.Prefixes.Select(p => builder.Regex(x => x.FullDocument.EventStream, $"^{Regex.Escape(p)}")));
        }

        return builder.In(x => x.FullDocument.EventStream, filter.Prefixes);
    }

    public static IEnumerable<StoredEvent> Filtered(this MongoEventCommit commit, ParsedStreamPosition position)
    {
        var eventStreamOffset = commit.EventStreamOffset;

        var commitTimestamp = commit.Timestamp;
        var commitOffset = 0;

        foreach (var @event in commit.Events)
        {
            eventStreamOffset++;

            if (commitOffset > position.CommitOffset || commitTimestamp > position.Timestamp)
            {
                var eventData = @event.ToEventData();
                var eventPosition = new ParsedStreamPosition(commitTimestamp, commitOffset, commit.Events.Length);

                yield return new StoredEvent(commit.EventStream, eventPosition, eventStreamOffset, eventData);
            }

            commitOffset++;
        }
    }

    public static IEnumerable<StoredEvent> Filtered(this MongoEventCommit commit)
    {
        return commit.Filtered(EventsVersion.Empty);
    }

    public static IEnumerable<StoredEvent> Filtered(this MongoEventCommit commit, long position)
    {
        var eventStreamOffset = commit.EventStreamOffset;

        var commitTimestamp = commit.Timestamp;
        var commitOffset = 0;

        foreach (var @event in commit.Events)
        {
            eventStreamOffset++;

            if (eventStreamOffset > position)
            {
                var eventData = @event.ToEventData();
                var eventPosition = new ParsedStreamPosition(commitTimestamp, commitOffset, commit.Events.Length);

                yield return new StoredEvent(commit.EventStream, eventPosition, eventStreamOffset, eventData);
            }

            commitOffset++;
        }
    }
}
