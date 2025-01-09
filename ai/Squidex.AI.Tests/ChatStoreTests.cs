// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FluentAssertions;
using Squidex.AI.Implementation;
using Xunit;

namespace Squidex.AI;

public abstract class ChatStoreTests
{
    public abstract Task<IChatStore> CreateSutAsync();

    [Fact]
    public async Task Should_return_null_if_item_not_found()
    {
        var sut = await CreateSutAsync();

        var entity = await sut.GetAsync(Guid.NewGuid().ToString(), default);

        Assert.Null(entity);
    }

    [Fact]
    public async Task Should_insert_conversation()
    {
        var sut = await CreateSutAsync();

        var conversationId = Guid.NewGuid().ToString();
        var conversation = new Conversation
        {
            History =
            [
                new ChatMessage
                {
                    Content = "Content1",
                    Type = ChatMessageType.Assistant,
                    TokenCount = 1,
                },
                new ChatMessage
                {
                    Content = "Content2",
                    Type = ChatMessageType.User,
                    TokenCount = 2,
                },
            ],
            ToolData = new Dictionary<string, string>
            {
                ["Key"] = "Value"
            }
        };

        await sut.StoreAsync(conversationId, conversation, DateTime.UtcNow, default);

        var entity = await sut.GetAsync(conversationId, default);

        entity.Should().BeEquivalentTo(conversation);
    }

    [Fact]
    public async Task Should_update_conversation()
    {
        var sut = await CreateSutAsync();

        var conversationId = Guid.NewGuid().ToString();
        var conversation0 = new Conversation();
        var conversation1 = new Conversation
        {
            History =
            [
                new ChatMessage
                {
                    Content = "Content1",
                    Type = ChatMessageType.Assistant,
                    TokenCount = 1,
                },
                new ChatMessage
                {
                    Content = "Content2",
                    Type = ChatMessageType.User,
                    TokenCount = 2,
                },
            ],
            ToolData = new Dictionary<string, string>
            {
                ["Key"] = "Value"
            }
        };

        await sut.StoreAsync(conversationId, conversation0, DateTime.UtcNow, default);
        await sut.StoreAsync(conversationId, conversation1, DateTime.UtcNow, default);

        var entity = await sut.GetAsync(conversationId, default);

        entity.Should().BeEquivalentTo(conversation1);
    }

    [Fact]
    public async Task Should_query_by_date()
    {
        var sut = await CreateSutAsync();

        var baseId = Guid.NewGuid().ToString();

        var conversation0Id = $"0_{baseId}";
        var conversation0 = new Conversation();
        var date0 = DateTime.UtcNow.AddDays(1);

        var conversation1Id = $"1_{baseId}";
        var conversation1 = new Conversation();
        var date1 = DateTime.UtcNow.AddDays(2);

        var conversation2Id = $"2_{baseId}";
        var conversation2 = new Conversation();
        var date2 = DateTime.UtcNow.AddDays(3);

        await sut.StoreAsync(conversation0Id, conversation0, date0, default);
        await sut.StoreAsync(conversation1Id, conversation1, date1, default);
        await sut.StoreAsync(conversation2Id, conversation2, date2, default);

        var entity = await sut.QueryAsync(date2, default).ToListAsync();

        var ids = entity.Select(x => x.Id).Where(x => x.Contains(baseId, StringComparison.Ordinal)).ToList();

        ids.Should().BeEquivalentTo([conversation0Id, conversation1Id]);
    }
}
