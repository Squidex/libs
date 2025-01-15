// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Squidex.Events;

public abstract record HeaderValue
{
    public static implicit operator HeaderValue(string source)
    {
        return new HeaderStringValue(source);
    }

    public static implicit operator HeaderValue(long source)
    {
        return new HeaderNumberValue(source);
    }

    public static implicit operator HeaderValue(bool source)
    {
        return new HeaderBooleanValue(source);
    }
}

public record HeaderBooleanValue(bool Value) : HeaderValue
{
    public override string ToString()
    {
        return Value ? "true" : "false";
    }
}

public record HeaderNumberValue(long Value) : HeaderValue
{
    public override string ToString()
    {
        return Value.ToString();
    }
}

public record HeaderStringValue(string Value) : HeaderValue
{
    public override string ToString()
    {
        return Value;
    }
}
