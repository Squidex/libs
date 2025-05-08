// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows;

public sealed class FlowStepPropertyDescriptor
{
    public string Editor { get; set; }

    public string Name { get; set; }

    public string Display { get; set; }

    public string? Description { get; set; }

    public string[]? Options { get; set; }

    public string? ObsoleteReason { get; set; }

    public bool IsFormattable { get; set; }

    public bool IsScript { get; set; }

    public bool IsRequired { get; set; }

    public bool IsObsolete { get; set; }
}
