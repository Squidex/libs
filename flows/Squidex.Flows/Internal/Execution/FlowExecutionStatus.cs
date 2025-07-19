// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.Internal.Execution;

public enum FlowExecutionStatus
{
    Pending,
    Scheduled,
    Completed,
    Failed,
    Running,
    Cancelled,
}
