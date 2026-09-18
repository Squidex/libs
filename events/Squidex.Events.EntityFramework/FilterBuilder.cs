// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Squidex.Events.EntityFramework;

internal static class FilterBuilder
{
    private const char LikeEscape = '\\';
    private static readonly ParameterExpression CommitParameterType = Expression.Parameter(typeof(EFEventCommit));
    private static readonly MemberExpression EventStreamMember = Expression.Property(CommitParameterType, nameof(EFEventCommit.EventStream));
    private static readonly MethodInfo DbLikeMethod = typeof(DbFunctionsExtensions).GetMethod("Like", [typeof(DbFunctions), typeof(string), typeof(string), typeof(string)])!;
    private static readonly ConstantExpression DbFunctions = Expression.Constant(EF.Functions);
    private static readonly ConstantExpression DbLikeEscape = Expression.Constant(LikeEscape.ToString());

    // EF Core inlines a ConstantExpression into the SQL as a literal, but turns a member access on a
    // captured object into a parameter, the same way it treats a compiler generated closure. Without
    // this every distinct prefix would produce its own SQL string and therefore its own entry in the
    // EF compiled query cache and in the database plan cache.
    private sealed class Capture(string value)
    {
        public string Value { get; } = value;
    }

    private static Expression Parameterize(string value)
    {
        return Expression.Property(Expression.Constant(new Capture(value)), nameof(Capture.Value));
    }

    public static IQueryable<EFEventCommit> WhereCommited(this IQueryable<EFEventCommit> q)
    {
        return q.Where(x => x.Position != null);
    }

    public static IQueryable<EFEventCommit> WhereTimestampAfter(this IQueryable<EFEventCommit> q, DateTime timestamp)
    {
        if (timestamp == default)
        {
            return q;
        }

        return q.Where(x => x.Timestamp >= timestamp);
    }

    public static IQueryable<EFEventCommit> WherePositionBefore(this IQueryable<EFEventCommit> q, long offset)
    {
        if (offset <= EventsVersion.Empty)
        {
            return q;
        }

        return q.Where(x => x.EventStreamOffset < offset);
    }

    public static IQueryable<EFEventCommit> WherePositionAfter(this IQueryable<EFEventCommit> q, long offset)
    {
        if (offset <= EventsVersion.Empty)
        {
            return q;
        }

        return q.Where(x => x.EventStreamOffset >= offset);
    }

    public static IQueryable<EFEventCommit> WherePositionAfter(this IQueryable<EFEventCommit> q, ParsedStreamPosition position)
    {
        if (position.IsEndOfCommit)
        {
            return q.Where(x => x.Position > position.Position);
        }

        return q.Where(x => x.Position >= position.Position);
    }

    public static IQueryable<EFEventCommit> WhereStreamMatches(this IQueryable<EFEventCommit> q, StreamFilter filter)
    {
        if (filter.Prefixes == null || filter.Prefixes.Count == 0)
        {
            return q;
        }

        if (filter.Kind == StreamFilterKind.MatchStart)
        {
            Expression combinedExpression = null!;
            foreach (var prefix in filter.Prefixes)
            {
                var like = Expression.Call(DbLikeMethod, DbFunctions, EventStreamMember, Parameterize(ToLikePattern(prefix)), DbLikeEscape);

                combinedExpression = combinedExpression == null ?
                    like :
                    Expression.OrElse(combinedExpression, like);
            }

            return q.Where(Expression.Lambda<Func<EFEventCommit, bool>>(combinedExpression!, CommitParameterType));
        }

        return q.Where(x => filter.Prefixes.Contains(x.EventStream));
    }

    public static string ToLikePattern(string prefix)
    {
        var sb = new StringBuilder(prefix.Length + 2);

        // A leading '%' is a wildcard for the first segment of the stream name, everything else is matched literally.
        var literal = prefix;
        if (literal.StartsWith('%'))
        {
            sb.Append('%');
            literal = literal[1..];
        }

        foreach (var c in literal)
        {
            if (c is LikeEscape or '%' or '_' or '[')
            {
                sb.Append(LikeEscape);
            }

            sb.Append(c);
        }

        sb.Append('%');
        return sb.ToString();
    }

    public static IEnumerable<StoredEvent> Filtered(this EFEventCommit commit, ParsedStreamPosition position)
    {
        if (!commit.Position.HasValue)
        {
            yield break;
        }

        var eventStreamOffset = commit.EventStreamOffset;

        var commitPosition = commit.Position.Value;
        var commitOffset = 0;

        foreach (var @event in commit.Events)
        {
            eventStreamOffset++;

            // The offset within the commit is only relevant for the commit the position points to.
            if (commitPosition > position.Position || (commitPosition == position.Position && commitOffset > position.CommitOffset))
            {
                var eventData = EventData.DeserializeFromJson(@event);
                var eventPosition = new ParsedStreamPosition(commitPosition, commitOffset, commit.Events.Length);

                yield return new StoredEvent(commit.EventStream, eventPosition, eventStreamOffset, eventData);
            }

            commitOffset++;
        }
    }

    public static IEnumerable<StoredEvent> FilteredReverse(this EFEventCommit commit, long position)
    {
        if (!commit.Position.HasValue)
        {
            yield break;
        }

        var commitPosition = commit.Position.Value;

        for (var commitOffset = commit.Events.Length - 1; commitOffset >= 0; commitOffset--)
        {
            var eventStreamOffset = commit.EventStreamOffset + commitOffset + 1;

            if (eventStreamOffset > position)
            {
                var eventData = EventData.DeserializeFromJson(commit.Events[commitOffset]);
                var eventPosition = new ParsedStreamPosition(commitPosition, commitOffset, commit.Events.Length);

                yield return new StoredEvent(commit.EventStream, eventPosition, eventStreamOffset, eventData);
            }
        }
    }

    public static IEnumerable<StoredEvent> Filtered(this EFEventCommit commit, long position)
    {
        if (!commit.Position.HasValue)
        {
            yield break;
        }

        var eventStreamOffset = commit.EventStreamOffset;

        var commitPosition = commit.Position.Value;
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
