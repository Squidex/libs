// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.RegularExpressions;
using MongoDB.Driver;

namespace Squidex.Events.Mongo;

internal abstract class QueryStrategy
{
    protected static readonly SortDefinitionBuilder<MongoEventCommit> Sort =
        Builders<MongoEventCommit>.Sort;

    protected static readonly FilterDefinitionBuilder<MongoEventCommit> Filter =
        Builders<MongoEventCommit>.Filter;

    protected static readonly FilterDefinitionBuilder<ChangeStreamDocument<MongoEventCommit>> FilterInStream =
        Builders<ChangeStreamDocument<MongoEventCommit>>.Filter;

    public abstract Task InitializeAsync(IMongoCollection<MongoEventCommit> collection,
        CancellationToken ct);

    public abstract IEnumerable<StoredEvent> Filtered(MongoEventCommit commit, ParsedStreamPosition position);

    public abstract IEnumerable<StoredEvent> Filtered(MongoEventCommit commit, long position);

    public abstract SortDefinition<MongoEventCommit> SortAscending();

    public abstract SortDefinition<MongoEventCommit> SortDescending();

    public abstract FilterDefinition<MongoEventCommit> ByFilter(StreamFilter filter, ParsedStreamPosition streamPosition);

    public virtual FilterDefinition<MongoEventCommit> ByNameAfter(string name, long streamPosition)
    {
        return Filter.And(ByStream(StreamFilter.Name(name)), Filter.Gte(x => x.EventStreamOffset, streamPosition));
    }

    public virtual FilterDefinition<MongoEventCommit> ByNameBefore(string name, long streamPosition)
    {
        return Filter.And(ByStream(StreamFilter.Name(name)), Filter.Lt(x => x.EventStreamOffset, streamPosition));
    }

    public virtual FilterDefinition<ChangeStreamDocument<MongoEventCommit>> ByFilterInStream(StreamFilter filter)
    {
        if (filter.Prefixes == null)
        {
            return FilterInStream.Exists(x => x.FullDocument.EventStream);
        }

        if (filter.Kind == StreamFilterKind.MatchStart)
        {
            return FilterInStream.Or(filter.Prefixes.Select(p => FilterInStream.Regex(x => x.FullDocument.EventStream, $"^{Regex.Escape(p)}")));
        }

        return FilterInStream.In(x => x.FullDocument.EventStream, filter.Prefixes);
    }

    public virtual Task CompleteAsync(Guid[] ids,
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    protected static FilterDefinition<MongoEventCommit> ByStream(StreamFilter filter)
    {
        var builder = Builders<MongoEventCommit>.Filter;

        static FilterDefinition<MongoEventCommit> Buildregex(string prefix, FilterDefinitionBuilder<MongoEventCommit> builder)
        {
            if (prefix.StartsWith('%'))
            {
                prefix = $"([a-zA-Z0-9]+){prefix[1..]}";
            }

            return builder.Regex(x => x.EventStream, $"^{prefix}");
        }

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
}
