// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Squidex.AI.Implementation;

namespace Squidex.AI;

public class ChatCleanerTests
{
    private readonly TimeProvider timeProvider = A.Fake<TimeProvider>();
    private readonly IChatAgent chatAgent = A.Fake<IChatAgent>();
    private readonly IChatTool tool1 = A.Fake<IChatTool>();
    private readonly IChatTool tool2 = A.Fake<IChatTool>();
    private readonly IChatStore chatStore = A.Fake<IChatStore>();
    private readonly ChatOptions options = new ChatOptions();
    private readonly ChatCleaner sut;

    public ChatCleanerTests()
    {
        var now = DateTimeOffset.UtcNow;

        A.CallTo(() => timeProvider.GetUtcNow())
            .Returns(now);

        A.CallTo(() => chatAgent.IsConfigured)
            .Returns(true);

        sut = new ChatCleaner(chatAgent, chatStore, [tool1, tool2], Options.Create(options), timeProvider,
            A.Fake<ILogger<ChatCleaner>>());
    }

    [Fact]
    public async Task Should_not_do_anything_if_agent_not_configured()
    {
        A.CallTo(() => chatAgent.IsConfigured)
            .Returns(false);

        await sut.CleanupAsync(default);

        A.CallTo(chatStore)
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Should_delete_conversations()
    {
        var olderThan = timeProvider.GetUtcNow() - options.ConversationLifetime;

        var conversationId1 = Guid.NewGuid().ToString();
        var conversation1 = new Conversation();
        var conversationId2 = Guid.NewGuid().ToString();
        var conversation2 = new Conversation();

        A.CallTo(() => chatStore.QueryAsync(olderThan.UtcDateTime, default))
            .Returns(
                new[]
                {
                    (conversationId1, conversation1),
                    (conversationId2, conversation2),
                }.ToAsyncEnumerable());

        await sut.CleanupAsync(default);

        A.CallTo(() => chatStore.RemoveAsync(conversationId1, default))
            .MustHaveHappened();

        A.CallTo(() => chatStore.RemoveAsync(conversationId2, default))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Should_call_tools_conversations()
    {
        var olderThan = timeProvider.GetUtcNow() - options.ConversationLifetime;

        var conversationId1 = Guid.NewGuid().ToString();
        var conversation1 = new Conversation();
        var conversationId2 = Guid.NewGuid().ToString();
        var conversation2 = new Conversation();

        A.CallTo(() => chatStore.QueryAsync(olderThan.UtcDateTime, default))
            .Returns(
                new[]
                {
                    (conversationId1, conversation1),
                    (conversationId2, conversation2),
                }.ToAsyncEnumerable());

        await sut.CleanupAsync(default);

        A.CallTo(() => tool1.CleanupAsync(A<Dictionary<string, string>>._, default))
            .MustHaveHappenedTwiceExactly();

        A.CallTo(() => tool2.CleanupAsync(A<Dictionary<string, string>>._, default))
            .MustHaveHappenedTwiceExactly();
    }
}
