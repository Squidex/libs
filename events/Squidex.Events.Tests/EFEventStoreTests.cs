// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squidex.Events.EntityFramework;
using Xunit;

namespace Squidex.Events;

public abstract class EFEventStoreTests : EventStoreTests
{
    public abstract IServiceProvider Services { get; }

    [Fact]
    public async Task Should_detect_primary_key_violation()
    {
        var ts = DateTime.UtcNow.Ticks + 1000;

        var dbAdapter = Services.GetRequiredService<IProviderAdapter>();
        var dbFactory = Services.GetRequiredService<IDbContextFactory<TestContext>>();

        await InsertTestValueAsync(dbFactory, ts, ts);

        // Same primary key, different unique value.
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => InsertTestValueAsync(dbFactory, ts, ts + 1));

        Assert.True(dbAdapter.IsDuplicateException(ex));
    }

    [Fact]
    public async Task Should_detect_index_violation()
    {
        var ts = DateTime.UtcNow.Ticks + 2000;

        var dbAdapter = Services.GetRequiredService<IProviderAdapter>();
        var dbFactory = Services.GetRequiredService<IDbContextFactory<TestContext>>();

        await InsertTestValueAsync(dbFactory, ts, ts);

        // Different primary key, same unique value.
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => InsertTestValueAsync(dbFactory, ts + 1, ts));

        Assert.True(dbAdapter.IsDuplicateException(ex));
    }

    private static async Task InsertTestValueAsync(IDbContextFactory<TestContext> dbContextFactory, long id, long value)
    {
        await using var dbContext1 = await dbContextFactory.CreateDbContextAsync();

        dbContext1.Tests.Add(new TestEntity { Id = id, UniqueValue = value });
        await dbContext1.SaveChangesAsync();
    }

    [Fact]
    public async Task Should_initialize_adapter_twice()
    {
        var dbAdapter = Services.GetRequiredService<IProviderAdapter>();
        var dbFactory = Services.GetRequiredService<IDbContextFactory<TestContext>>();

        for (var i = 0; i < 2; i++)
        {
            await using var dbContext = await dbFactory.CreateDbContextAsync();
            await dbAdapter.InitializeAsync(dbContext, default);
        }
    }

    [Fact]
    public async Task Should_calculate_positions()
    {
        var dbAdapter = Services.GetRequiredService<IProviderAdapter>();
        var dbFactory = Services.GetRequiredService<IDbContextFactory<TestContext>>();

        var values = new HashSet<long>();

        for (var i = 0; i < 1000; i++)
        {
            await using var dbContext = await dbFactory.CreateDbContextAsync();
            await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();

            var position = await dbAdapter.GetPositionAsync(dbContext, default);
            await dbTransaction.CommitAsync();
            values.Add(position);
        }

        Assert.Equal(1000, values.Count);
    }

    [Fact]
    public async Task Should_calculate_positions_in_parallel()
    {
        var dbAdapter = Services.GetRequiredService<IProviderAdapter>();
        var dbFactory = Services.GetRequiredService<IDbContextFactory<TestContext>>();

        var values = new ConcurrentDictionary<long, long>();

        await Parallel.ForEachAsync(Enumerable.Range(0, 1000), async (_, ct) =>
        {
            await using var dbContext = await dbFactory.CreateDbContextAsync(ct);
            await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();

            var position = await dbAdapter.GetPositionAsync(dbContext, default);
            await dbTransaction.CommitAsync();
            values.TryAdd(position, position);
        });

        Assert.Equal(1000, values.Count);
    }
}
