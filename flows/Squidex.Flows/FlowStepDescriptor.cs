﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows;

public sealed class FlowStepDescriptor
{
    public Type Type { get; set; }

    public string Title { get; set; }

    public string ReadMore { get; set; }

    public string IconImage { get; set; }

    public string IconColor { get; set; }

    public string Display { get; set; }

    public string Description { get; set; }

    public bool IsObsolete { get; set; }

    public string? ObsoleteReason { get; set; }

    public List<FlowStepPropertyDescriptor> Properties { get; } = [];
}
