// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI;

public sealed class DelegateChatTool(ToolSpec spec, Func<ToolContext, CancellationToken, Task<string>> action) : IChatTool
{
    public ToolSpec Spec { get; } = spec;

    public Task<string> ExecuteAsync(ToolContext toolContext,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(toolContext);

        return action(toolContext, ct);
    }
}
