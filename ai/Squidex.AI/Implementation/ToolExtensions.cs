// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;

namespace Squidex.AI.Implementation;

public static class ToolExtensions
{
    public static async Task<string> PromptAsync(this IChatProvider chatProvider, string prompt, ToolContext toolContext,
        CancellationToken ct)
    {
        var buffer = new StringBuilder();

        var request = new ChatProviderRequest
        {
            ChatAgent = toolContext.ChatAgent,
            Context = toolContext.Context,
            Tool = null,
            ToolData = [],
            Tools = [],
            History =
            [
                new ChatMessage
                {
                    Type = ChatMessageType.System,
                    Content = prompt
                },
            ]
        };

        var stream = chatProvider.StreamAsync(request, ct);
        await foreach (var @event in stream)
        {
            if (@event is ChunkEvent chunkEvent)
            {
                buffer.Append(chunkEvent.Content);
            }
        }

        return buffer.ToString();
    }
}
