// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows;

public interface IFlowStepRegistry
{
    IReadOnlyDictionary<string, FlowStepDescriptor> Steps { get; }
}
