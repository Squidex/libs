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

internal record struct ParsedStreamPosition(BsonTimestamp Timestamp, long GlobalPosition, long CommitOffset, long CommitSize)
{
    public static readonly ParsedStreamPosition Start = new ParsedStreamPosition(new BsonTimestamp(0, 0), 0, -1, -1);

    public readonly bool IsEndOfCommit => CommitOffset == CommitSize - 1;

    public static implicit operator StreamPosition(ParsedStreamPosition position)
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

            return new StreamPosition(sb.ToString(), false);
        }
        finally
        {
            DefaultPools.StringBuilder.Return(sb);
        }
    }

    public static implicit operator ParsedStreamPosition(StreamPosition value)
    {
        var token = value.Token;
        if (string.IsNullOrWhiteSpace(token))
        {
            return Start;
        }

        var parts = token.Split('-');
        if (parts.Length == 4)
        {
            var culture = CultureInfo.InvariantCulture;
            if (!int.TryParse(parts[0], NumberStyles.Integer, culture, out var timestamp) ||
                !int.TryParse(parts[1], NumberStyles.Integer, culture, out var increment) ||
                !int.TryParse(parts[2], NumberStyles.Integer, culture, out var commitOffset) ||
                !int.TryParse(parts[3], NumberStyles.Integer, culture, out var commitSize))
            {
                return default;
            }

            return new ParsedStreamPosition(
                new BsonTimestamp(timestamp, increment),
                0,
                commitOffset,
                commitSize);
        }

        if (parts.Length == 3)
        {
            var culture = CultureInfo.InvariantCulture;
            if (!int.TryParse(parts[1], NumberStyles.Integer, culture, out var globalPosition) ||
                !int.TryParse(parts[2], NumberStyles.Integer, culture, out var commitOffset) ||
                !int.TryParse(parts[3], NumberStyles.Integer, culture, out var commitSize))
            {
                return default;
            }

            return new ParsedStreamPosition(
                new BsonTimestamp(0, 0),
                globalPosition,
                commitOffset,
                commitSize);
        }

        return Start;
    }

    public static implicit operator ParsedStreamPosition(DateTime timestamp)
    {
        if (timestamp == default)
        {
            return Start;
        }

        return new DateTimeOffset(timestamp, default);
    }

    public static implicit operator ParsedStreamPosition(DateTimeOffset timestamp)
    {
        if (timestamp == default)
        {
            return Start;
        }

        return new ParsedStreamPosition(new BsonTimestamp((int)timestamp.ToUnixTimeSeconds(), 0), 0, 0, 0);
    }
}
