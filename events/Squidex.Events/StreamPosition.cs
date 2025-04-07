// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Squidex.Events;

public record struct StreamPosition(string? Token, bool ReadFromEnd)
{
    public static readonly StreamPosition Start = default;

    public static readonly StreamPosition End = new StreamPosition(null, true);

    public static implicit operator StreamPosition(string? value)
    {
        return new StreamPosition(value, false);
    }

    public static implicit operator string?(StreamPosition source)
    {
        return source.Token;
    }
}
