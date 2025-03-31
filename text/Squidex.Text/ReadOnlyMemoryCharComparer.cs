// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics.CodeAnalysis;

namespace Squidex.Text;

internal sealed class ReadOnlyMemoryCharComparer(StringComparison comparison) : IEqualityComparer<ReadOnlyMemory<char>>
{
    public static readonly ReadOnlyMemoryCharComparer Ordinal
        = new ReadOnlyMemoryCharComparer(StringComparison.Ordinal);

    public static readonly ReadOnlyMemoryCharComparer OrdinalIgnoreCase
        = new ReadOnlyMemoryCharComparer(StringComparison.OrdinalIgnoreCase);

    public static readonly ReadOnlyMemoryCharComparer InvariantCulture
        = new ReadOnlyMemoryCharComparer(StringComparison.InvariantCulture);

    public static readonly ReadOnlyMemoryCharComparer InvariantCultureIgnoreCase
        = new ReadOnlyMemoryCharComparer(StringComparison.InvariantCultureIgnoreCase);

    public static readonly ReadOnlyMemoryCharComparer CurrentCulture
        = new ReadOnlyMemoryCharComparer(StringComparison.CurrentCulture);

    public static readonly ReadOnlyMemoryCharComparer CurrentCultureIgnoreCase
        = new ReadOnlyMemoryCharComparer(StringComparison.CurrentCultureIgnoreCase);

    public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
    {
        return x.Span.Equals(y.Span, comparison);
    }

    public int GetHashCode([DisallowNull] ReadOnlyMemory<char> obj)
    {
        return string.GetHashCode(obj.Span, comparison);
    }
}
