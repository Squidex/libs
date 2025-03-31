// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.Implementation;

public sealed class SingleChatToolProvider<T>(T tool) : IChatToolProvider where T : IChatTool
{
    public IAsyncEnumerable<IChatTool> GetToolsAsync(ChatContext chatContext,
        CancellationToken ct)
    {
        return AsyncEnumerable.Repeat<IChatTool>(tool, 1);
    }
}
