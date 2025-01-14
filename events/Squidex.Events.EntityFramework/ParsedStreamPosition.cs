// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
#pragma warning disable RECS0082 // Parameter has the same name as a member and hides it

namespace Squidex.Events.EntityFramework;

internal record struct ParsedStreamPosition(long Position, long CommitOffset, long CommitSize)
{
    public static readonly ParsedStreamPosition Start = new ParsedStreamPosition(0, -1, -1);

    public readonly bool IsEndOfCommit => CommitOffset == CommitSize - 1;

    public static implicit operator StreamPosition(ParsedStreamPosition position)
    {
        var sb = DefaultPools.StringBuilder.Get();
        try
        {
            sb.Append(position.Position);
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
        if (parts.Length != 3)
        {
            return Start;
        }

        var culture = CultureInfo.InvariantCulture;
        if (!int.TryParse(parts[0], NumberStyles.Integer, culture, out var position) ||
            !int.TryParse(parts[1], NumberStyles.Integer, culture, out var commitOffset) ||
            !int.TryParse(parts[2], NumberStyles.Integer, culture, out var commitSize))
        {
            return default;
        }

        return new ParsedStreamPosition(position, commitOffset, commitSize);
    }
}
