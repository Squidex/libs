// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FluentAssertions;
using Xunit;

namespace Squidex.Messaging;

public abstract class MessagingDataStoreTests
{
    protected abstract Task<IMessagingDataStore> CreateSutAsync();

    [Fact]
    public async Task Should_insert_and_return_item()
    {
        var sut = await CreateSutAsync();

        var entry1 = new Entry("Group1", "Key1", new SerializedObject([1, 2, 3], "Type1", "Format1"), DateTime.UtcNow.AddDays(3));
        var entry2 = new Entry("Group2", "Key2", new SerializedObject([4, 5, 6], "Type2", "Format2"), DateTime.UtcNow.AddDays(4));

        await sut.StoreManyAsync([entry1, entry2], default);

        var found = await sut.GetEntriesAsync("Group1", default);

        found.Single().Should().BeEquivalentTo(entry1, options =>
            options.Using<DateTime>(c => c.Subject.Should().BeCloseTo(c.Expectation, TimeSpan.FromSeconds(1)))
                .WhenTypeIs<DateTime>());
    }

    [Fact]
    public async Task Should_not_return_expired_item()
    {
        var sut = await CreateSutAsync();

        var entry1 = new Entry("Group1", "Key1", new SerializedObject([1, 2, 3], "Type1", "Format1"), DateTime.UtcNow.AddDays(-3));
        var entry2 = new Entry("Group2", "Key2", new SerializedObject([4, 5, 6], "Type2", "Format2"), DateTime.UtcNow.AddDays(4));

        await sut.StoreManyAsync([entry1, entry2], default);

        var found = await sut.GetEntriesAsync("Group1", default);

        Assert.Empty(found);
    }

    [Fact]
    public async Task Should_delete_item()
    {
        var sut = await CreateSutAsync();

        var entry1 = new Entry("Group1", "Key1", new SerializedObject([1, 2, 3], "Type1", "Format1"), DateTime.UtcNow.AddDays(3));
        var entry2 = new Entry("Group1", "Key2", new SerializedObject([4, 5, 6], "Type2", "Format2"), DateTime.UtcNow.AddDays(4));

        await sut.StoreManyAsync([entry1, entry2], default);
        await sut.DeleteAsync("Group1", "Key2", default);

        var found = await sut.GetEntriesAsync("Group1", default);

        found.Single().Should().BeEquivalentTo(entry1, options =>
            options.Using<DateTime>(c => c.Subject.Should().BeCloseTo(c.Expectation, TimeSpan.FromSeconds(1)))
                .WhenTypeIs<DateTime>());
    }
}
