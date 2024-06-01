// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.Implementation;

public sealed class SingleChatToolProvider<T> : IChatToolProvider where T : IChatTool
{
    private readonly T tool;

    public SingleChatToolProvider(T tool)
    {
        this.tool = tool;
    }

    public IAsyncEnumerable<IChatTool> GetToolsAsync(ChatContext chatContext,
        CancellationToken ct)
    {
        return AsyncEnumerable.Repeat<IChatTool>(tool, 1);
    }
}
