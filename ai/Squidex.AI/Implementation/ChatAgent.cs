// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Squidex.AI.Implementation;

public sealed class ChatAgent : IChatAgent
{
    private readonly ChatOptions options;
    private readonly IChatProvider chatProvider;
    private readonly IChatStore chatStore;
    private readonly List<IChatTool> chatTools;

    public bool IsConfigured => chatProvider is not NoopChatProvider;

    public ChatAgent(
        IOptions<ChatOptions> options,
        IChatProvider chatProvider,
        IChatStore chatStore,
        IEnumerable<IChatTool> chatTools)
    {
        this.options = options.Value;
        this.chatProvider = chatProvider;
        this.chatStore = chatStore;
        this.chatTools = chatTools.ToList();
    }

    public Task StopConversationAsync(string conversationId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);

        return chatStore.RemoveAsync(conversationId, ct);
    }

    public async Task<ChatResult> PromptAsync(ChatRequest request, ChatContext? context = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Prompt);

        var streamMeta = new ChatMetadata();
        var streamContent = new StringBuilder();

        await foreach (var message in StreamAsync(request, context, ct))
        {
            switch (message)
            {
                case ChunkEvent c:
                    streamContent.Append(c.Content);
                    break;
                case MetadataEvent m:
                    streamMeta = m.Metadata;
                    break;
            }
        }

        return new ChatResult { Content = streamContent.ToString(), Metadata = streamMeta };
    }

    public IAsyncEnumerable<ChatEvent> StreamAsync(ChatRequest request, ChatContext? context = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Prompt);

        return StreamCoreAsync(request, context, ct);
    }

    private async IAsyncEnumerable<ChatEvent> StreamCoreAsync(ChatRequest request, ChatContext? context = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            yield break;
        }

        context ??= new ChatContext();

        ChatConfiguration? configuration = null;
        if (request.Configuration != null)
        {
            options.Configurations?.TryGetValue(request.Configuration, out configuration);
        }

        configuration ??= options.Defaults;
        configuration ??= new ChatConfiguration();

        var history = await GetOrCreateConversationAsync(request, configuration, ct);

        var providerRequest = new ChatProviderRequest
        {
            Agent = this,
            Context = context,
            Tools = GetTools(configuration),
            Tool = request.Tool,
            History = history,
        };

        var streamCosts = 0m;
        var streamContent = new StringBuilder();

        await foreach (var @event in chatProvider.StreamAsync(providerRequest, ct).WithCancellation(ct))
        {
            if (@event is ChatFinishEvent f)
            {
                streamCosts += f.NumInputTokens * options.PricePerInputTokenInEUR;
                streamCosts += f.NumOutputTokens * options.PricePerOutputTokenInEUR;

                yield return new MetadataEvent
                {
                    Metadata = new ChatMetadata
                    {
                        CostsInEUR = streamCosts,
                        NumInputTokens = f.NumInputTokens,
                        NumOutputTokens = f.NumOutputTokens,
                    }
                };
            }

            if (@event is ChunkEvent chunkEvent)
            {
                streamContent.Append(chunkEvent.Content);
            }

            if (@event is ToolStartEvent toolStart)
            {
                if (options.ToolCostsInEur?.TryGetValue(toolStart.Tool.Spec.DisplayName, out var costs) == true)
                {
                    streamCosts += costs;
                }
            }

            if (@event is ChatEvent publicEvent)
            {
                yield return publicEvent;
            }
        }

        history.Add(streamContent.ToString(), ChatMessageType.Assistant);

        if (request.ConversationId != null)
        {
            await StoreHistoryAsync(request.ConversationId, history);
        }
    }

    private List<IChatTool> GetTools(ChatConfiguration configuration)
    {
        var tools = chatTools;

        if (configuration.Tools != null)
        {
            tools = tools.Where(x => configuration.Tools.Contains(x.Spec.Name)).ToList();
        }

        return tools;
    }

    private async Task StoreHistoryAsync(string conversationId, ChatHistory history)
    {
        await chatStore.StoreAsync(conversationId, JsonSerializer.Serialize(history), default);
    }

    private async Task<ChatHistory> GetOrCreateConversationAsync(ChatRequest request, ChatConfiguration configuration,
        CancellationToken ct)
    {
        ChatHistory? history = null;

        if (request.ConversationId != null)
        {
            var stored = await chatStore.GetAsync(request.ConversationId, ct);
            if (stored != null)
            {
                history = JsonSerializer.Deserialize<ChatHistory>(stored) ??
                    throw new ChatException($"Cannot deserialize conversion with ID '{request.ConversationId}'.");
            }
        }

        if (history == null)
        {
            history = [];

            if (configuration.SystemMessages != null)
            {
                foreach (var systemMessage in configuration.SystemMessages)
                {
                    history.Add(systemMessage, ChatMessageType.System);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Prompt))
        {
            history.Add(request.Prompt, ChatMessageType.User);
        }

        return history;
    }
}
