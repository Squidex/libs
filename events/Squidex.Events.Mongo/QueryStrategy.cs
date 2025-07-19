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

    protected static readonly FilterDefinitionBuilder<MongoEventCommit> Filters =
        Builders<MongoEventCommit>.Filter;

    protected static readonly FilterDefinitionBuilder<ChangeStreamDocument<MongoEventCommit>> FiltersIsStream =
        Builders<ChangeStreamDocument<MongoEventCommit>>.Filter;

    public abstract Task InitializeAsync(IMongoCollection<MongoEventCommit> collection,
        CancellationToken ct);

    public abstract IEnumerable<StoredEvent> Filtered(MongoEventCommit commit, ParsedStreamPosition position);

    public abstract IEnumerable<StoredEvent> Filtered(MongoEventCommit commit, long position);

    public abstract SortDefinition<MongoEventCommit> SortAscending();

    public abstract SortDefinition<MongoEventCommit> SortDescending();

    public abstract FilterDefinition<MongoEventCommit> FilterAfter(StreamFilter filter, ParsedStreamPosition streamPosition);

    public virtual FilterDefinition<MongoEventCommit> FilterAfter(string name, long streamPosition)
    {
        return Filters.And(ByStream(StreamFilter.Name(name)), Filters.Gte(x => x.EventStreamOffset, streamPosition));
    }

    public virtual FilterDefinition<MongoEventCommit> FilterBefore(string name, long streamPosition)
    {
        return Filters.And(ByStream(StreamFilter.Name(name)), Filters.Lt(x => x.EventStreamOffset, streamPosition));
    }

    public virtual FilterDefinition<MongoEventCommit> Filter(StreamFilter filter)
    {
        return ByStream(filter);
    }

    public virtual FilterDefinition<ChangeStreamDocument<MongoEventCommit>> ByFilterInStream(StreamFilter filter)
    {
        if (filter.Prefixes == null)
        {
            return FiltersIsStream.Exists(x => x.FullDocument.EventStream);
        }

        if (filter.Kind == StreamFilterKind.MatchStart)
        {
            return FiltersIsStream.Or(filter.Prefixes.Select(p => FiltersIsStream.Regex(x => x.FullDocument.EventStream, $"^{Regex.Escape(p)}")));
        }

        return FiltersIsStream.In(x => x.FullDocument.EventStream, filter.Prefixes);
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
