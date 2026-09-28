// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Squidex.Events.EntityFramework;

namespace Squidex.Events;

public abstract class EFEventStoreTests<TDbContext> : EventStoreTests where TDbContext : TestDbContext
{
    public abstract IServiceProvider Services { get; }

    [Fact]
    public async Task Should_detect_primary_key_violation()
    {
        var ts = DateTime.UtcNow.Ticks + 1000;

        var dbAdapter = Services.GetRequiredService<IProviderAdapter>();
        var dbFactory = Services.GetRequiredService<IDbContextFactory<TDbContext>>();

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
        var dbFactory = Services.GetRequiredService<IDbContextFactory<TDbContext>>();

        await InsertTestValueAsync(dbFactory, ts, ts);

        // Different primary key, same unique value.
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => InsertTestValueAsync(dbFactory, ts + 1, ts));

        Assert.True(dbAdapter.IsDuplicateException(ex));
    }

    [Fact]
    public async Task Should_append_to_stream_blocked_by_stale_commit_without_position()
    {
        var sut = await CreateSutAsync();

        var dbFactory = Services.GetRequiredService<IDbContextFactory<TDbContext>>();

        var streamName = $"test-{Guid.NewGuid()}";

        // Left over from a crash of an older version between insert and position update.
        await using (var dbContext = await dbFactory.CreateDbContextAsync())
        {
            dbContext.Set<EFEventCommit>().Add(new EFEventCommit
            {
                Id = Guid.NewGuid(),
                Events = [CreateEventData(0).SerializeToJsonString()],
                EventsCount = 1,
                EventStream = streamName,
                EventStreamOffset = EventsVersion.Empty,
                Timestamp = DateTime.UtcNow.AddHours(-2),
            });

            await dbContext.SaveChangesAsync();
        }

        var commit = new[] { CreateEventData(1) };

        await sut.AppendAsync(Guid.NewGuid(), streamName, EventsVersion.Empty, commit);

        var readEvents = await sut.QueryStreamAsync(streamName);

        Assert.Equal(["Type1"], readEvents.Select(x => x.Data.Type));
    }

    [Fact]
    public async Task Should_not_leave_commit_without_position_if_append_is_cancelled()
    {
        using var cts = new CancellationTokenSource();

        var adapter = new CancellingAdapter(Services.GetRequiredService<IProviderAdapter>(), cts);

        var sut = new EFEventStore<TDbContext>(
            Services.GetRequiredService<IDbContextFactory<TDbContext>>(),
            Services.GetRequiredService<IDbEventStoreBulkInserter>(),
            adapter,
            TimeProvider.System,
            Services.GetRequiredService<IOptions<EFEventStoreOptions>>());

        var streamName = $"test-{Guid.NewGuid()}";

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.AppendAsync(Guid.NewGuid(), streamName, EventsVersion.Empty, [CreateEventData(1)], cts.Token));

        await using var dbContext = await Services.GetRequiredService<IDbContextFactory<TDbContext>>().CreateDbContextAsync();

        Assert.False(await dbContext.Set<EFEventCommit>().AnyAsync(x => x.EventStream == streamName));
    }

    private sealed class CancellingAdapter(IProviderAdapter inner, CancellationTokenSource cts) : IProviderAdapter
    {
        public Task InitializeAsync(DbContext dbContext, CancellationToken ct)
        {
            return inner.InitializeAsync(dbContext, ct);
        }

        public async Task<long> UpdatePositionAsync(DbContext dbContext, Guid id, CancellationToken ct)
        {
            await cts.CancelAsync();
            throw new OperationCanceledException(cts.Token);
        }

        public Task<long> UpdatePositionsAsync(DbContext dbContext, Guid[] ids, CancellationToken ct)
        {
            return inner.UpdatePositionsAsync(dbContext, ids, ct);
        }

        public bool IsDuplicateException(Exception exception)
        {
            return inner.IsDuplicateException(exception);
        }
    }

    private static async Task InsertTestValueAsync(IDbContextFactory<TDbContext> dbContextFactory, long id, long value)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        dbContext.Tests.Add(new TestEntity { Id = id, UniqueValue = value });
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Should_initialize_adapter_twice()
    {
        var dbAdapter = Services.GetRequiredService<IProviderAdapter>();
        var dbFactory = Services.GetRequiredService<IDbContextFactory<TDbContext>>();

        for (var i = 0; i < 2; i++)
        {
            await using var dbContext = await dbFactory.CreateDbContextAsync();
            await dbAdapter.InitializeAsync(dbContext, default);
        }
    }

    [Fact]
    public async Task Should_update_position()
    {
        var dbAdapter = Services.GetRequiredService<IProviderAdapter>();
        var dbFactory = Services.GetRequiredService<IDbContextFactory<TDbContext>>();

        var values = new HashSet<long>();

        for (var i = 0; i < 1000; i++)
        {
            await using var dbContext = await dbFactory.CreateDbContextAsync();

            values.Add(await dbAdapter.UpdatePositionAsync(dbContext, Guid.NewGuid(), default));
        }

        Assert.Equal(1000, values.Count);
    }

    [Fact]
    public async Task Should_update_position_in_parallel()
    {
        var dbAdapter = Services.GetRequiredService<IProviderAdapter>();
        var dbFactory = Services.GetRequiredService<IDbContextFactory<TDbContext>>();

        var values = new ConcurrentDictionary<long, long>();

        await Parallel.ForEachAsync(Enumerable.Range(0, 1000), async (_, ct) =>
        {
            await using var dbContext = await dbFactory.CreateDbContextAsync(ct);
            await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                values.TryAdd(await dbAdapter.UpdatePositionAsync(dbContext, Guid.NewGuid(), default), 0);
                await dbTransaction.CommitAsync(ct);
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync(ct);
                throw;
            }
        });

        Assert.Equal(1000, values.Count);
    }

    [Fact]
    public async Task Should_update_and_assign_position()
    {
        var dbAdapter = Services.GetRequiredService<IProviderAdapter>();
        var dbFactory = Services.GetRequiredService<IDbContextFactory<TDbContext>>();

        var commit_0 = new EFEventCommit
        {
            Id = Guid.NewGuid(),
            Events = [],
            EventsCount = 0,
            EventStream = Guid.NewGuid().ToString(),
            EventStreamOffset = 0,
        };

        long position = 0;
        await using (var dbContext = await dbFactory.CreateDbContextAsync())
        {
            await dbContext.Set<EFEventCommit>().AddAsync(commit_0);
            await dbContext.SaveChangesAsync();

            position = await dbAdapter.UpdatePositionAsync(dbContext, commit_0.Id, default);
        }

        await using (var dbContext = await dbFactory.CreateDbContextAsync())
        {
            var commit_1 = await dbContext.Set<EFEventCommit>().Where(x => x.Id == commit_0.Id).SingleAsync();

            Assert.Equal(position, commit_1.Position);
        }
    }

    [Fact]
    public async Task Should_update_positions()
    {
        var dbAdapter = Services.GetRequiredService<IProviderAdapter>();
        var dbFactory = Services.GetRequiredService<IDbContextFactory<TDbContext>>();

        Guid[] ids =
        [
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
        ];

        var values = new HashSet<long>();

        for (var i = 0; i < 1000; i++)
        {
            await using var dbContext = await dbFactory.CreateDbContextAsync();

            values.Add(await dbAdapter.UpdatePositionsAsync(dbContext, ids, default));
        }

        var ordered = values.Order().ToList();

        Assert.Equal(1000, values.Count);
        Assert.All(ordered.Skip(1).Take(10), (value, i) => Assert.Equal(ids.Length, value - ordered[i]));
    }

    [Fact]
    public async Task Should_update_positions_in_parallel()
    {
        var dbAdapter = Services.GetRequiredService<IProviderAdapter>();
        var dbFactory = Services.GetRequiredService<IDbContextFactory<TDbContext>>();

        Guid[] ids =
        [
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
        ];

        var values = new ConcurrentDictionary<long, long>();

        await Parallel.ForEachAsync(Enumerable.Range(0, 1000), async (_, ct) =>
        {
            await using var dbContext = await dbFactory.CreateDbContextAsync(ct);
            await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                values.TryAdd(await dbAdapter.UpdatePositionsAsync(dbContext, ids, default), 0);
                await dbTransaction.CommitAsync(ct);
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync(ct);
                throw;
            }
        });

        var ordered = values.Keys.Order().ToList();

        Assert.Equal(1000, values.Count);
        Assert.All(ordered.Skip(1).Take(10), (value, i) => Assert.Equal(ids.Length, value - ordered[i]));
    }

    [Fact]
    public async Task Should_update_and_assign_positions()
    {
        var dbAdapter = Services.GetRequiredService<IProviderAdapter>();
        var dbFactory = Services.GetRequiredService<IDbContextFactory<TDbContext>>();

        var commit1_0 = new EFEventCommit
        {
            Id = Guid.NewGuid(),
            Events = [],
            EventsCount = 0,
            EventStream = Guid.NewGuid().ToString(),
            EventStreamOffset = 0,
        };

        var commit2_0 = new EFEventCommit
        {
            Id = Guid.NewGuid(),
            Events = [],
            EventsCount = 0,
            EventStream = Guid.NewGuid().ToString(),
            EventStreamOffset = 0,
        };

        long position = 0;
        await using (var dbContext = await dbFactory.CreateDbContextAsync())
        {
            await dbContext.Set<EFEventCommit>().AddAsync(commit1_0);
            await dbContext.Set<EFEventCommit>().AddAsync(commit2_0);
            await dbContext.SaveChangesAsync();

            position = await dbAdapter.UpdatePositionsAsync(dbContext, [commit1_0.Id, commit2_0.Id], default);
        }

        await using (var dbContext = await dbFactory.CreateDbContextAsync())
        {
            var commit1_1 = await dbContext.Set<EFEventCommit>().Where(x => x.Id == commit1_0.Id).SingleAsync();
            var commit2_1 = await dbContext.Set<EFEventCommit>().Where(x => x.Id == commit2_0.Id).SingleAsync();

            Assert.Equal(position - 1, commit1_1.Position);
            Assert.Equal(position + 0, commit2_1.Position);
        }
    }
}
