// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using System.Runtime.CompilerServices;
using EventStore.Client;
using ESStreamPosition = EventStore.Client.StreamPosition;

namespace Squidex.Events.GetEventStore;

public static class Extensions
{
    public static bool Is<T>(this Exception ex) where T : Exception
    {
        if (ex is AggregateException aggregateException)
        {
            aggregateException = aggregateException.Flatten();

            return aggregateException.InnerExceptions.Count == 1 && Is<T>(aggregateException.InnerExceptions[0]);
        }

        return ex is T;
    }

    public static StreamRevision ToRevision(this long version)
    {
        return StreamRevision.FromInt64(version);
    }

    public static ESStreamPosition ToPositionBefore(this long version)
    {
        if (version < 0)
        {
            return ESStreamPosition.Start;
        }

        return ESStreamPosition.FromInt64(version + 1);
    }

    public static ESStreamPosition ToPosition(this StreamPosition position, bool inclusive)
    {
        if (string.IsNullOrWhiteSpace(position))
        {
            return ESStreamPosition.Start;
        }

        if (long.TryParse(position.Token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPosition))
        {
            if (!inclusive)
            {
                parsedPosition++;
            }

            return ESStreamPosition.FromInt64(parsedPosition);
        }

        return ESStreamPosition.Start;
    }

    public static async IAsyncEnumerable<StoredEvent> IgnoreNotFound(this IAsyncEnumerable<StoredEvent> source,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var enumerator = source.GetAsyncEnumerator(ct);

        bool resultFound;
        try
        {
            resultFound = await enumerator.MoveNextAsync(ct);
        }
        catch (StreamNotFoundException)
        {
            resultFound = false;
        }

        if (!resultFound)
        {
            yield break;
        }

        yield return enumerator.Current;

        while (await enumerator.MoveNextAsync(ct))
        {
            ct.ThrowIfCancellationRequested();

            yield return enumerator.Current;
        }
    }

    public static string ToRegex(this StreamFilter filter)
    {
        if (filter.Prefixes == null || filter.Prefixes.Length == 0)
        {
            return ".*";
        }

        if (filter.Kind == StreamFilterKind.MatchStart)
        {
            return $"^({string.Join('|', filter.Prefixes.Select(p => $"({p})"))})";
        }
        else
        {
            return $"^({string.Join('|', filter.Prefixes.Select(p => $"({p})"))})$";
        }
    }
}
