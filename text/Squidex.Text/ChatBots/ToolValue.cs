// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Squidex.Text.ChatBots;

public abstract record ToolValue
{
    public static readonly ToolNullValue Null = new ToolNullValue();
}

public sealed record ToolStringValue(string Value) : ToolValue;

public sealed record ToolNumberValue(double Value) : ToolValue;

public sealed record ToolBooleanValue(bool Value) : ToolValue;

public sealed record ToolNullValue : ToolValue;
