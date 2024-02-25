// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;

namespace Squidex.Text.ChatBots.OpenAI;

public sealed class OpenAIChatAgent : IChatAgent
{
    private const int MaxToolRuns = 5;
    private readonly IChatStore chatStore;
    private readonly Dictionary<string, IChatTool> chatTools;
    private readonly OpenAIChatBotOptions options;
    private readonly List<ToolDefinition> tools = [];
    private OpenAIService? service;

    public bool IsConfigured { get; }

    public OpenAIChatAgent(IOptions<OpenAIChatBotOptions> options, IChatStore chatStore,
        IEnumerable<IChatTool> chatTools)
    {
        this.options = options.Value;
        this.chatStore = chatStore;
        this.chatTools = chatTools.ToDictionary(x => x.Spec.Name);

        IsConfigured = !string.IsNullOrWhiteSpace(options.Value.ApiKey);

        if (!IsConfigured)
        {
            return;
        }

        tools.AddRange(chatTools.Select(x => x.Spec.ToToolDefinition()));
    }

    public Task StopConversationAsync(string conversationId,
        CancellationToken ct = default)
    {
        return chatStore.ClearAsync(conversationId, ct);
    }

    public async Task<ChatBotResponse> PromptAsync(string conversationId, string prompt,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return ChatBotResponse.Failed("Agent is not enabled.");
        }

        var request = await GetOrCreateConversationAsync(conversationId, ct);

        if (!request.AppendMessage(ChatMessage.FromUser(prompt), options.MaxContextLength, options.CharactersPerToken))
        {
            return ChatBotResponse.Failed("Input is too large for the agent.");
        }

        return await RequestAsync(conversationId, request, ct);
    }

    private async Task<ChatBotResponse> RequestAsync(string conversationId, ChatCompletionCreateRequest request,
        CancellationToken ct)
    {
        service ??= new OpenAIService(options);

        async Task<(ChatBotResponse, int)> RequestCoreAsync()
        {
            var numTokens = 0;
            for (var run = 0; run < MaxToolRuns; run++)
            {
                var response = await service.ChatCompletion.CreateCompletion(request, cancellationToken: ct);

                numTokens += response.Usage?.PromptTokens ?? 0;
                numTokens += response.Usage?.CompletionTokens ?? 0;

                if (response.Error != null)
                {
                    return (ChatBotResponse.Failed(response.Error.Message ?? "Unknown error."), numTokens);
                }

                var choice = response.Choices[0].Message;

                request.Messages.Add(choice);

                if (choice.ToolCalls is not { Count: > 0 })
                {
                    return (ChatBotResponse.Success(choice.Content!), numTokens);
                }

                var validCalls = new List<(IChatTool Tool, int Index, string Id, FunctionCall Call)>();

                var i = 0;
                foreach (var call in choice.ToolCalls)
                {
                    if (string.IsNullOrWhiteSpace(call.FunctionCall?.Name))
                    {
                        return (ChatBotResponse.Failed("Tool has no function name."), numTokens);
                    }

                    if (!chatTools.TryGetValue(call.FunctionCall.Name, out var tool))
                    {
                        return (ChatBotResponse.Failed($"Tool has unknown function name '{call.FunctionCall.Name}'."), numTokens);
                    }

                    validCalls.Add((tool, i++, call.Id, call.FunctionCall));
                }

                var results = new ChatMessage[validCalls.Count];

                // Run all the tools in parallel, because they could take long time potentially.
                await Parallel.ForEachAsync(validCalls, ct, async (job, ct) =>
                {
                    var result = await job.Tool.ExecuteAsync(job.Call.ParseArguments(job.Tool.Spec), ct);

                    results[job.Index] = ChatMessage.FromTool(result, job.Id);
                });

                foreach (var result in results)
                {
                    request.Messages.Add(result);
                }
            }

            return (ChatBotResponse.Failed("Exceeded max tool runs."), numTokens);
        }

        var (result, numTokens) = await RequestCoreAsync();

        var conversation = new Conversation
        {
            Messages = request.Messages.ToList(),
        };

        await chatStore.StoreAsync(conversationId, JsonSerializer.Serialize(conversation), default);

        return result with
        {
            EstimatedCostsInEUR = numTokens * options.PricePerInputTokenInEUR
        };
    }

    private async Task<ChatCompletionCreateRequest> GetOrCreateConversationAsync(string conversationId,
        CancellationToken ct)
    {
        var request = new ChatCompletionCreateRequest
        {
            MaxTokens = options.MaxAnswerTokens,
            Messages = [],
            Model = options.Model,
            Temperature = options.Temperature,
            Tools = tools.Count > 0 ? tools : null,
            N = 1
        };

        var stored = await chatStore.GetAsync(conversationId, ct);

        if (stored != null)
        {
            var conversation = JsonSerializer.Deserialize<Conversation>(stored) ??
                throw new ChatException($"Cannot deserialize conversion with ID '{conversationId}'.");

            foreach (var message in conversation.Messages)
            {
                request.Messages.Add(message);
            }
        }
        else if (options.SystemMessages != null)
        {
            foreach (var systemMessage in options.SystemMessages)
            {
                request.Messages.Add(ChatMessage.FromSystem(systemMessage));
            }
        }

        return request;
    }

    private sealed class Conversation
    {
        public List<ChatMessage> Messages { get; set; } = [];
    }
}
