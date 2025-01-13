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

internal record struct StreamPosition(long Position, long CommitOffset, long CommitSize)
{
    public static readonly StreamPosition Start = new StreamPosition(0, -1, -1);

    public readonly bool IsEndOfCommit => CommitOffset == CommitSize - 1;

    public static implicit operator string(StreamPosition position)
    {
        var sb = DefaultPools.StringBuilder.Get();
        try
        {
            sb.Append(position.Position);
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

        return new StreamPosition(position, commitOffset, commitSize);
    }
}
