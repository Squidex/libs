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
    ILogger<ChatCleaner> log) : BackgroundProcess
{
    protected override async Task ExecuteAsync(
        CancellationToken ct)
    {
        if (!chatAgent.IsConfigured)
        {
            return;
        }

        if (options.Value.CleanupTime <= TimeSpan.Zero)
        {
            log.LogInformation("Skipping cleanup, because cleanup time is less or equal than zero.");
            return;
        }

        var timer = new PeriodicTimer(options.Value.CleanupTime);
        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                await CleanupAsync(ct);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Failed to execute timer.");
            }
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
