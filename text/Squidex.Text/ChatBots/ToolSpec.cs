// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Squidex.Text.ChatBots;

public sealed record ToolSpec(string Name, string Description)
{
    public ToolArgumentSpec[] Arguments { get; init; } = [];
}

public abstract record ToolArgumentSpec(string Name, string Description)
{
    public bool IsRequired { get; init; }
}

public sealed record ToolBooleanArgumentSpec(string Name, string Description) : ToolArgumentSpec(Name, Description)
{
}

public sealed record ToolNumberArgumentSpec(string Name, string Description) : ToolArgumentSpec(Name, Description)
{
}

public sealed record ToolStringArgumentSpec(string Name, string Description) : ToolArgumentSpec(Name, Description)
{
}

public sealed record ToolEnumArgumentSpec(string Name, string Description) : ToolArgumentSpec(Name, Description)
{
    required public string[] Values { get; set; }
}
