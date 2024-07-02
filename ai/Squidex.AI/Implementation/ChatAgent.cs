// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Options;

namespace Squidex.AI.Implementation;

public sealed class ChatAgent : IChatAgent
{
    private readonly ChatOptions options;
    private readonly IChatProvider chatProvider;
    private readonly IChatStore chatStore;
    private readonly IEnumerable<IChatPipe> chatPipes;
    private readonly IEnumerable<IChatToolProvider> chatToolProviders;

    public bool IsConfigured => chatProvider is not NoopChatProvider;

    public ChatAgent(
        IChatProvider chatProvider,
        IChatStore chatStore,
        IEnumerable<IChatPipe> chatPipes,
        IEnumerable<IChatToolProvider> chatToolProviders,
        IOptions<ChatOptions> options)
    {
        this.options = options.Value;
        this.chatPipes = chatPipes;
        this.chatProvider = chatProvider;
        this.chatStore = chatStore;
        this.chatToolProviders = chatToolProviders;
    }

    public async Task StopConversationAsync(string conversationId, ChatContext? context = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);

        var conversation = await chatStore.GetAsync(conversationId, ct);

        if (conversation == null)
        {
            return;
        }

        await chatStore.RemoveAsync(conversationId, ct);

        foreach (var provider in chatToolProviders)
        {
            context ??= new ChatContext();

            await foreach (var tool in provider.GetToolsAsync(context, ct))
            {
                await tool.CleanupAsync(conversation.ToolData, ct);
            }
        }
    }

    public async Task<ChatResult> PromptAsync(ChatRequest request, ChatContext? context = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contents = new StringBuilder();
        var toolStarts = new List<ToolStartEvent>();
        var toolEnds = new List<ToolEndEvent>();
        var metadata = new ChatMetadata();

        await foreach (var message in StreamAsync(request, context, ct))
        {
            switch (message)
            {
                case ChunkEvent c:
                    contents.Append(c.Content);
                    break;
                case MetadataEvent m:
                    metadata = m.Metadata;
                    break;
                case ToolStartEvent s:
                    toolStarts.Add(s);
                    break;
                case ToolEndEvent e:
                    toolEnds.Add(e);
                    break;
            }
        }

        var content = contents.ToString();

        return new ChatResult
        {
            Content = content,
            ToolStarts = toolStarts,
            ToolEnds = toolEnds,
            Metadata = metadata,
        };
    }

    public IAsyncEnumerable<ChatEvent> StreamAsync(ChatRequest request, ChatContext? context = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

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

        var conversation = await GetOrCreateConversationAsync(request, configuration, ct);

        var providerRequest = new ChatProviderRequest
        {
            ChatAgent = this,
            Context = context,
            History = conversation.History,
            Tool = request.Tool,
            Tools = await GetToolsAsync(configuration, context, ct),
            ToolData = conversation.ToolData,
        };

        var streamCosts = 0m;
        var streamContent = new StringBuilder();

        var stream = chatProvider.StreamAsync(providerRequest, ct);

        foreach (var pipe in chatPipes)
        {
            stream = pipe.StreamAsync(stream, providerRequest, ct);
        }

        await foreach (var @event in stream.WithCancellation(ct))
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

        conversation.History.Add(streamContent.ToString(), ChatMessageType.Assistant);

        if (request.ConversationId != null)
        {
            await StoreHistoryAsync(request.ConversationId, conversation, default);
        }
    }

    private async Task<List<IChatTool>> GetToolsAsync(ChatConfiguration configuration, ChatContext context,
        CancellationToken ct)
    {
        var tools = new List<IChatTool>();

        foreach (var toolProvider in chatToolProviders)
        {
            await foreach (var tool in toolProvider.GetToolsAsync(context, ct))
            {
                if (configuration.Tools?.Contains(tool.Spec.Name) != false)
                {
                    tools.Add(tool);
                }
            }
        }

        return tools;
    }

    private Task StoreHistoryAsync(string conversationId, Conversation conversation,
        CancellationToken ct)
    {
        return chatStore.StoreAsync(conversationId, conversation, ct);
    }

    private async Task<Conversation> GetOrCreateConversationAsync(ChatRequest request, ChatConfiguration configuration,
        CancellationToken ct)
    {
        Conversation? result = null;

        if (request.ConversationId != null)
        {
            result = await chatStore.GetAsync(request.ConversationId, ct);
        }

        if (result == null)
        {
            result = new Conversation { History = [], ToolData = [] };

            if (configuration.SystemMessages != null)
            {
                foreach (var systemMessage in configuration.SystemMessages)
                {
                    result.History.Add(systemMessage, ChatMessageType.System);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Prompt))
        {
            result.History.Add(request.Prompt, ChatMessageType.User);
        }

        return result;
    }
}
