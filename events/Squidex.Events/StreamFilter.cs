// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Events;

public readonly struct StreamFilter(StreamFilterKind kind, IReadOnlySet<string>? prefixes = null) : IEquatable<StreamFilter>
{
    public IReadOnlySet<string>? Prefixes { get; } = prefixes;

    public StreamFilterKind Kind { get; } = kind;

    public static StreamFilter Prefix(params string[] prefixes)
    {
        return new StreamFilter(StreamFilterKind.MatchStart, prefixes?.ToHashSet());
    }

    public static StreamFilter Name(params string[] prefixes)
    {
        return new StreamFilter(StreamFilterKind.MatchFull, prefixes?.ToHashSet());
    }

    public static StreamFilter All()
    {
        return default;
    }

    public static bool operator ==(StreamFilter lhs, StreamFilter rhs)
    {
        return lhs.Equals(rhs);
    }

    public static bool operator !=(StreamFilter lhs, StreamFilter rhs)
    {
        return !lhs.Equals(rhs);
    }

    public override bool Equals(object? obj)
    {
        return obj is StreamFilter other && Equals(other);
    }

    public bool Equals(StreamFilter other)
    {
        if (Kind != other.Kind)
        {
            return false;
        }

        if ((Prefixes == null || Prefixes.Count == 0) && (other.Prefixes == null || other.Prefixes.Count == 0))
        {
            return true;
        }

        if (Prefixes == null || other.Prefixes == null)
        {
            return false;
        }

        return Prefixes?.SetEquals(other.Prefixes) == true;
    }

    public override int GetHashCode()
    {
        var hashCode = 17 * Kind.GetHashCode();

        if (Prefixes != null)
        {
            foreach (var prefix in Prefixes)
            {
                hashCode ^= 23 * prefix.GetHashCode(StringComparison.Ordinal);
            }
        }

        return hashCode;
    }
}

public enum StreamFilterKind
{
    MatchFull,
    MatchStart
}
