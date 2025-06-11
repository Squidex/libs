// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using Squidex.AI.Implementation;

namespace Squidex.AI;

public class ChatAgentTests
{
    private readonly IChatProvider chatProvider = A.Fake<IChatProvider>();
    private readonly IChatStore chatStore = A.Fake<IChatStore>();
    private readonly ChatAgent sut;

    public ChatAgentTests()
    {
        sut = new ChatAgent(chatProvider, chatStore, [], [], Options.Create(new ChatOptions()));
    }

    [Fact]
    public async Task Should_provider_history_without_system_messages_if_requested()
    {
        var conversationId = Guid.NewGuid().ToString();
        var conversation = new Conversation
        {
            History =
            [
                new ChatMessage { Content = "Message1", Type = ChatMessageType.User },
                new ChatMessage { Content = "Message2", Type = ChatMessageType.System },
                new ChatMessage { Content = "Message3", Type = ChatMessageType.Assistant },
            ],
        };

        A.CallTo(() => chatStore.GetAsync(conversationId, default))
            .Returns(conversation);

        var request = new ChatRequest { LoadHistory = true, ConversationId = conversationId };
        var result = await sut.StreamAsync(request).ToListAsync();

        result.Should().BeEquivalentTo(
            [
                new ChatHistoryLoaded { Message = conversation.History[0] },
                new ChatHistoryLoaded { Message = conversation.History[2] },
            ],
            o => o.PreferringDeclaredMemberTypes());

        A.CallTo(chatProvider)
            .MustNotHaveHappened();
    }
}
