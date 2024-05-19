// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;

#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Squidex.AI;

public abstract record ToolValue
{
    public static readonly ToolNullValue Null = new ToolNullValue();

    public virtual string AsString
    {
        get => throw new InvalidOperationException($"Expected 'String', got '{GetType().Name}'");
    }

    public virtual double AsNumber
    {
        get => throw new InvalidOperationException($"Expected 'Number', got '{GetType().Name}'");
    }

    public virtual bool AsBoolean
    {
        get => throw new InvalidOperationException($"Expected 'Boolean', got '{GetType().Name}'");
    }

    public virtual bool IsNull
    {
        get => false;
    }
}

public sealed record ToolStringValue(string Value) : ToolValue
{
    public override string AsString => Value;

    public override string ToString()
    {
        return Value;
    }
}

public sealed record ToolNumberValue(double Value) : ToolValue
{
    public override double AsNumber => Value;

    public override string ToString()
    {
        return Value.ToString(CultureInfo.InvariantCulture);
    }
}

public sealed record ToolBooleanValue(bool Value) : ToolValue
{
    public override bool AsBoolean => Value;

    public override string ToString()
    {
        return Value.ToString(CultureInfo.InvariantCulture);
    }
}

public sealed record ToolNullValue : ToolValue
{
    public override bool IsNull => true;

    public override string ToString()
    {
        return "null";
    }
}
