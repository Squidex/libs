// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI;

public interface IChatTool
{
    ToolSpec Spec { get; }

    Task<string> ExecuteAsync(IChatAgent agent, ChatContext context, Dictionary<string, ToolValue> arguments,
        CancellationToken ct);
}
