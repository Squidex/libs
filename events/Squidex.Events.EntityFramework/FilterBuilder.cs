// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Squidex.Events.EntityFramework;

internal static class FilterBuilder
{
    private static readonly ParameterExpression CommitParameterType = Expression.Parameter(typeof(EFEventCommit));
    private static readonly MemberExpression EventStreamMember = Expression.Property(CommitParameterType, nameof(EFEventCommit.EventStream));
    private static readonly MethodInfo DbLikeMethod = typeof(DbFunctionsExtensions).GetMethod("Like", [typeof(DbFunctions), typeof(string), typeof(string)])!;
    private static readonly ConstantExpression DbFunctions = Expression.Constant(EF.Functions);

    public static IQueryable<EFEventCommit> ByTimestamp(this IQueryable<EFEventCommit> q, DateTime timestamp)
    {
        if (timestamp == default)
        {
            return q;
        }

        return q.Where(x => x.Timestamp >= timestamp);
    }

    public static IQueryable<EFEventCommit> ByBeforeOffset(this IQueryable<EFEventCommit> q, long offset)
    {
        if (offset <= EtagVersion.Empty)
        {
            return q;
        }

        return q.Where(x => x.EventStreamOffset < offset);
    }

    public static IQueryable<EFEventCommit> ByOffset(this IQueryable<EFEventCommit> q, long offset)
    {
        if (offset <= EtagVersion.Empty)
        {
            return q;
        }

        return q.Where(x => x.EventStreamOffset >= offset);
    }

    public static IQueryable<EFEventCommit> ByPosition(this IQueryable<EFEventCommit> q, ParsedStreamPosition position)
    {
        if (position.IsEndOfCommit)
        {
            return q.Where(x => x.Position > position.Position);
        }

        return q.Where(x => x.Position >= position.Position);
    }

    public static IQueryable<EFEventCommit> ByStream(this IQueryable<EFEventCommit> q, StreamFilter filter)
    {
        if (filter.Prefixes == null || filter.Prefixes.Length == 0)
        {
            return q;
        }

        if (filter.Kind == StreamFilterKind.MatchStart)
        {
            Expression combinedExpression = null!;
            foreach (var prefix in filter.Prefixes)
            {
                var like = Expression.Call(DbLikeMethod, DbFunctions, EventStreamMember, Expression.Constant($"{prefix}%"));

                combinedExpression = combinedExpression == null ?
                    like :
                    Expression.OrElse(combinedExpression, like);
            }

            return q.Where(Expression.Lambda<Func<EFEventCommit, bool>>(combinedExpression!, CommitParameterType));
        }

        return q.Where(x => filter.Prefixes.Contains(x.EventStream));
    }

    public static IEnumerable<StoredEvent> Filtered(this EFEventCommit commit, ParsedStreamPosition position)
    {
        var eventStreamOffset = commit.EventStreamOffset;

        var commitPosition = commit.Position!.Value;
        var commitOffset = 0;

        foreach (var @event in commit.Events)
        {
            eventStreamOffset++;

            if (commitOffset > position.CommitOffset || commitPosition > position.Position)
            {
                var eventData = EventData.DeserializeFromJson(@event);
                var eventPosition = new ParsedStreamPosition(commitPosition, commitOffset, commit.Events.Length);

                yield return new StoredEvent(commit.EventStream, eventPosition, eventStreamOffset, eventData);
            }

            commitOffset++;
        }
    }

    public static IEnumerable<StoredEvent> Filtered(this EFEventCommit commit, long position)
    {
        var eventStreamOffset = commit.EventStreamOffset;

        var commitPosition = commit.Position!.Value;
        var commitOffset = 0;

        foreach (var @event in commit.Events)
        {
            eventStreamOffset++;

            if (eventStreamOffset > position)
            {
                var eventData = EventData.DeserializeFromJson(@event);
                var eventPosition = new ParsedStreamPosition(commitPosition, commitOffset, commit.Events.Length);

                yield return new StoredEvent(commit.EventStream, eventPosition, eventStreamOffset, eventData);
            }

            commitOffset++;
        }
    }
}
