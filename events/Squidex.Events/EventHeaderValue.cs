// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Events;

public readonly record struct HeaderValue
{
    public object? Value { get; }

    private HeaderValue(object? value)
    {
        Value = value;
    }

    public static implicit operator HeaderValue(string source)
    {
        return new HeaderValue(source);
    }

    public static implicit operator HeaderValue(double source)
    {
        return new HeaderValue(source);
    }

    public static implicit operator HeaderValue(bool source)
    {
        return new HeaderValue(source);
    }

    public override string ToString()
    {
        switch (Value)
        {
            case null:
                return "null";
            case true:
                return "true";
            case false:
                return "false";
            default:
                return Value.ToString()!;
        }
    }
}
