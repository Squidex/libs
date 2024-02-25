// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.ChatBots;

public interface IChatTool
{
    ToolSpec Spec { get; }

    Task<string> ExecuteAsync(Dictionary<string, ToolValue> arguments,
        CancellationToken ct);
}
