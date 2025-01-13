// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using MongoDB.Bson;
using Squidex.Events.Utils;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
#pragma warning disable RECS0082 // Parameter has the same name as a member and hides it

namespace Squidex.Events.Mongo;

internal record struct StreamPosition(BsonTimestamp Timestamp, long CommitOffset, long CommitSize)
{
    public static readonly StreamPosition Start = new StreamPosition(new BsonTimestamp(0, 0), -1, -1);

    public readonly bool IsEndOfCommit => CommitOffset == CommitSize - 1;

    public static implicit operator string(StreamPosition position)
    {
        var sb = DefaultPools.StringBuilder.Get();
        try
        {
            sb.Append(position.Timestamp.Timestamp);
            sb.Append('-');
            sb.Append(position.Timestamp.Increment);
            sb.Append('-');
            sb.Append(position.CommitOffset);
            sb.Append('-');
            sb.Append(position.CommitSize);

            return sb.ToString();
        }
        finally
        {
            DefaultPools.StringBuilder.Return(sb);
        }
    }

    public static implicit operator StreamPosition(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Start;
        }

        var parts = value.Split('-');
        if (parts.Length != 4)
        {
            return Start;
        }

        var culture = CultureInfo.InvariantCulture;
        if (!int.TryParse(parts[0], NumberStyles.Integer, culture, out var timestamp) ||
            !int.TryParse(parts[1], NumberStyles.Integer, culture, out var increment) ||
            !int.TryParse(parts[2], NumberStyles.Integer, culture, out var commitOffset) ||
            !int.TryParse(parts[3], NumberStyles.Integer, culture, out var commitSize))
        {
            return default;
        }

        return new StreamPosition(
            new BsonTimestamp(timestamp, increment),
            commitOffset,
            commitSize);
    }

    public static implicit operator StreamPosition(DateTime timestamp)
    {
        if (timestamp == default)
        {
            return Start;
        }

        return new StreamPosition(
                new BsonTimestamp((int)new DateTimeOffset(timestamp, default).ToUnixTimeSeconds(), 0),
                0,
                0);
    }
}
