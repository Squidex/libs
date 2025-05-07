// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Flows.Internal;

namespace Squidex.Flows;

public sealed class FlowValidationContext(IServiceProvider serviceProvider, FlowDefinition? definition)
{
    public IServiceProvider ServiceProvider => serviceProvider;

    public FlowDefinition? Definition => definition;
}
