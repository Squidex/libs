// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Squidex.AI.SemanticKernel;

internal sealed class OpenAIChatAgent : IChatAgent
{
    private readonly IChatStore store;
    private readonly TimeProvider timeProvider;
    private readonly Kernel kernel;
    private readonly OpenAIChatBotOptions options;

    public bool IsConfigured => kernel.Services.GetService<IChatCompletionService>() != null;

    public OpenAIChatAgent(Kernel kernel, IChatStore store, IOptions<OpenAIChatBotOptions> options,
        TimeProvider timeProvider)
    {
        this.kernel = kernel;
        this.store = store;
        this.timeProvider = timeProvider;
        this.options = options.Value;
    }

    public async Task<ChatBotResponse> PromptAsync(string prompt, string? conversationId = null,
        CancellationToken ct = default)
    {
        if (kernel == null)
        {
            return ChatBotResponse.Failed("Not configured.");
        }

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        var openAIPromptExecutionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            TopP = 1,
            Temperature = options.Temperature ?? 1
        };

        var history = await LoadHistoryAsync(conversationId, ct);

        if (!string.IsNullOrWhiteSpace(prompt))
        {
            history.AddUserMessage(prompt);
        }

        var result =
            await chatCompletionService.GetChatMessageContentsAsync(
                history,
                openAIPromptExecutionSettings,
                kernel,
                ct);

        history.AddRange(result);

        if (!string.IsNullOrWhiteSpace(conversationId))
        {
            await StoreHistoryAsync(history, conversationId, ct);
        }

        var content = result[0].Content ??
            throw new ChatException($"Chat does not return a result for ID '{conversationId ?? "none"}'.");

        return ChatBotResponse.Success(content) with
        {
            EstimatedCostsInEUR = CalculateCosts(result)
        };
    }

    private decimal CalculateCosts(IReadOnlyList<ChatMessageContent> result)
    {
        var costs = 0m;

        if (result[0].Metadata?.TryGetValue("Usage", out var m) == true && m is CompletionsUsage usage)
        {
            costs += usage.PromptTokens * options.PricePerInputTokenInEUR;
            costs += usage.CompletionTokens * options.PricePerOutputTokenInEUR;
        }

        return costs;
    }

    private async Task<ChatHistory> LoadHistoryAsync(string? conversationId,
        CancellationToken ct)
    {
        ChatHistory history;

        if (!string.IsNullOrWhiteSpace(conversationId))
        {
            var stored = await store.GetAsync(conversationId, ct);

            if (stored != null)
            {
                history = JsonSerializer.Deserialize<ChatHistory>(stored) ??
                    throw new ChatException($"Cannot deserialize conversion with ID '{conversationId}'.");

                return history;
            }
        }

        history = [];
        foreach (var systemMessage in options.SystemMessages ?? [])
        {
            history.AddSystemMessage(systemMessage);
        }

        return history;
    }

    private Task StoreHistoryAsync(ChatHistory history, string conversationId,
        CancellationToken ct)
    {
        var expires = timeProvider.GetLocalNow().UtcDateTime + options.ConversationLifetime;

        var json = JsonSerializer.Serialize(history) ??
            throw new ChatException($"Cannot serialize conversion with ID '{conversationId}'.");

        return store.StoreAsync(conversationId, json, expires, ct);
    }

    public Task StopConversationAsync(string conversationId,
        CancellationToken ct = default)
    {
        return store.GetAsync(conversationId, ct);
    }
}
