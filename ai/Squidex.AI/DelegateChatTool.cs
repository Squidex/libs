// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI;

public sealed class DelegateChatTool : IChatTool
{
    private readonly Func<ToolContext, CancellationToken, Task<string>> action;

    public ToolSpec Spec { get; }

    public DelegateChatTool(ToolSpec spec, Func<ToolContext, CancellationToken, Task<string>> action)
    {
        Spec = spec;

        this.action = action;
    }

    public Task<string> ExecuteAsync(ToolContext toolContext, CancellationToken ct)
    {
        return action(toolContext, ct);
    }
}
