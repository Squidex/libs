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
    private readonly string prefix = Guid.NewGuid().ToString();

    protected abstract Task<IMessagingDataStore> CreateSutAsync();

    [Fact]
    public async Task Should_insert_and_return_item()
    {
        var sut = await CreateSutAsync();

        var entry1 = new Entry($"{prefix}Group1", "Key1", new SerializedObject([1, 2, 3], "Type1", "Format1"), DateTime.UtcNow.AddDays(3));
        var entry2 = new Entry($"{prefix}Group2", "Key2", new SerializedObject([4, 5, 6], "Type2", "Format2"), DateTime.UtcNow.AddDays(4));

        await sut.StoreManyAsync([entry1, entry2], default);

        var found = await sut.GetEntriesAsync($"{prefix}Group1", default);

        found.Single().Should().BeEquivalentTo(entry1, options =>
            options.Using<DateTime>(c => c.Subject.Should().BeCloseTo(c.Expectation, TimeSpan.FromSeconds(1)))
                .WhenTypeIs<DateTime>());
    }

    [Fact]
    public async Task Should_insert_and_update_in_multiple_batches()
    {
        var sut = await CreateSutAsync();

        var entry1_1 = new Entry($"{prefix}Group1", "Key1", new SerializedObject([1, 2, 3], "Type1", "Format1"), DateTime.UtcNow.AddDays(3));
        var entry1_2 = new Entry($"{prefix}Group1", "Key1", new SerializedObject([4, 5, 6], "Type2", "Format2"), DateTime.UtcNow.AddDays(4));

        await sut.StoreManyAsync([entry1_1], default);
        await sut.StoreManyAsync([entry1_2], default);

        var found = await sut.GetEntriesAsync($"{prefix}Group1", default);

        found.Single().Should().BeEquivalentTo(entry1_2, options =>
            options.Using<DateTime>(c => c.Subject.Should().BeCloseTo(c.Expectation, TimeSpan.FromSeconds(1)))
                .WhenTypeIs<DateTime>());
    }

    [Fact]
    public async Task Should_insert_and_update_in_one_batch()
    {
        var sut = await CreateSutAsync();

        var entry1_1 = new Entry($"{prefix}Group1", "Key1", new SerializedObject([1, 2, 3], "Type1", "Format1"), DateTime.UtcNow.AddDays(3));
        var entry1_2 = new Entry($"{prefix}Group1", "Key1", new SerializedObject([4, 5, 6], "Type2", "Format2"), DateTime.UtcNow.AddDays(4));

        await sut.StoreManyAsync([entry1_1, entry1_2], default);

        var found = await sut.GetEntriesAsync($"{prefix}Group1", default);

        found.Single().Should().BeEquivalentTo(entry1_2, options =>
            options.Using<DateTime>(c => c.Subject.Should().BeCloseTo(c.Expectation, TimeSpan.FromSeconds(1)))
                .WhenTypeIs<DateTime>());
    }

    [Fact]
    public async Task Should_not_return_expired_item()
    {
        var sut = await CreateSutAsync();

        var entry1 = new Entry($"{prefix}Group1", "Key1", new SerializedObject([1, 2, 3], "Type1", "Format1"), DateTime.UtcNow.AddDays(-3));
        var entry2 = new Entry($"{prefix}Group2", "Key2", new SerializedObject([4, 5, 6], "Type2", "Format2"), DateTime.UtcNow.AddDays(4));

        await sut.StoreManyAsync([entry1, entry2], default);

        var found = await sut.GetEntriesAsync($"{prefix}Group1", default);

        Assert.Empty(found);
    }

    [Fact]
    public async Task Should_delete_item()
    {
        var sut = await CreateSutAsync();

        var entry1 = new Entry($"{prefix}Group1", "Key1", new SerializedObject([1, 2, 3], "Type1", "Format1"), DateTime.UtcNow.AddDays(3));
        var entry2 = new Entry($"{prefix}Group1", "Key2", new SerializedObject([4, 5, 6], "Type2", "Format2"), DateTime.UtcNow.AddDays(4));

        await sut.StoreManyAsync([entry1, entry2], default);
        await sut.DeleteAsync($"{prefix}Group1", "Key2", default);

        var found = await sut.GetEntriesAsync($"{prefix}Group1", default);

        found.Single().Should().BeEquivalentTo(entry1, options =>
            options.Using<DateTime>(c => c.Subject.Should().BeCloseTo(c.Expectation, TimeSpan.FromSeconds(1)))
                .WhenTypeIs<DateTime>());
    }
}
