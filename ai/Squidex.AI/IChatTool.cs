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

    Task<string> ExecuteAsync(ToolContext toolContext,
        CancellationToken ct);

    Task CleanupAsync(Dictionary<string, string> toolData,
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
