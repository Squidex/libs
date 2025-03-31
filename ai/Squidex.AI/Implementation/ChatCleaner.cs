// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Squidex.Hosting;

namespace Squidex.AI.Implementation;

public sealed class ChatCleaner(
    IChatAgent chatAgent,
    IChatStore chatStore,
    IEnumerable<IChatTool> chatTools,
    IOptions<ChatOptions> options,
    TimeProvider timeProvider,
    ILogger<ChatCleaner> log)
    : IBackgroundProcess
{
    private SimpleTimer? timer;

    public Task StartAsync(
        CancellationToken ct)
    {
        timer = new SimpleTimer(CleanupAsync, options.Value.CleanupTime, log);
        return Task.CompletedTask;
    }

    public async Task StopAsync(
        CancellationToken ct)
    {
        if (timer != null)
        {
            await timer.DisposeAsync();
            timer = null;
        }
    }

    public async Task CleanupAsync(
        CancellationToken ct)
    {
        if (!chatAgent.IsConfigured)
        {
            return;
        }

        var maxAge = timeProvider.GetUtcNow() - options.Value.ConversationLifetime;

        await foreach (var (id, conversation) in chatStore.QueryAsync(maxAge.UtcDateTime, ct))
        {
            await chatStore.RemoveAsync(id, ct);

            foreach (var tool in chatTools)
            {
                await tool.CleanupAsync(conversation.ToolData, ct);
            }
        }
    }
}
