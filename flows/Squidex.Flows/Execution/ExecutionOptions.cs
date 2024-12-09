// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.Execution;

public struct ExecutionOptions
{
    public bool IsSimulation { get; set; }

    public TimeSpan Timeout { get; set; }
}
