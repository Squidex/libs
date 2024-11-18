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
    ILogger<ChatCleaner> log) : IBackgroundProcess
{
    private readonly ChatOptions options = options.Value;
    private SimpleTimer? cleanupTimer;

    public Task StartAsync(
        CancellationToken ct)
    {
        // Just a guard when this method is called twice.
        cleanupTimer ??= new SimpleTimer(CleanupAsync, options.CleanupTime, log);

        return Task.CompletedTask;
    }

    public async Task StopAsync(
        CancellationToken ct)
    {
        if (cleanupTimer != null)
        {
            await cleanupTimer.DisposeAsync();

            cleanupTimer = null;
        }
    }

    public async Task CleanupAsync(
        CancellationToken ct)
    {
        if (!chatAgent.IsConfigured)
        {
            return;
        }

        var maxAge = timeProvider.GetUtcNow() - options.ConversationLifetime;

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
