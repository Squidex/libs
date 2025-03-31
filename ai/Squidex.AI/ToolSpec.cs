// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Squidex.AI;

public sealed record ToolSpec(string Name, string DisplayName, string Description)
{
    public Dictionary<string, ToolArgumentSpec> Arguments { get; init; } = [];
}

public abstract record ToolArgumentSpec(string Description)
{
    public bool IsRequired { get; init; }
}

public sealed record ToolBooleanArgumentSpec(string Description) : ToolArgumentSpec(Description)
{
}

public sealed record ToolNumberArgumentSpec(string Description) : ToolArgumentSpec(Description)
{
}

public sealed record ToolStringArgumentSpec(string Description) : ToolArgumentSpec(Description)
{
}

public sealed record ToolEnumArgumentSpec(string Description) : ToolArgumentSpec(Description)
{
    required public string[] Values { get; set; }
}
