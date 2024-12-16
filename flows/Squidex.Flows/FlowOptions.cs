// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows;

public class FlowOptions
{
    public TimeSpan JobQueryInterval { get; set; } = TimeSpan.FromSeconds(10);

    public List<Type> Steps { get; set; } = [];
}
