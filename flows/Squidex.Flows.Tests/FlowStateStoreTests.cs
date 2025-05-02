// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using NodaTime;
using Squidex.Flows.Internal.Execution;

namespace Squidex.Flows;

public abstract class FlowStateStoreTests
{
    private static readonly Instant FarInFuture = Instant.FromDateTimeOffset(new DateTimeOffset(3000, 12, 11, 10, 9, 8, TimeSpan.Zero));
    private int counter;

    protected abstract Task<IFlowStateStore<TestFlowContext>> CreateSutAsync();

    [Fact]
    public async Task Should_store_states()
    {
        var sut = await CreateSutAsync();

        var ownerId = Guid.NewGuid().ToString();
        var state1 = CreateState(ownerId);
        var state2 = CreateState(ownerId);

        await sut.StoreAsync([state1, state2]);

        var found = await sut.QueryByOwnerAsync(ownerId);

        found.Should().BeEquivalentTo((new List<FlowExecutionState<TestFlowContext>> { state1, state2 }, 2));
    }

    [Fact]
    public async Task Should_update_store_states()
    {
        var sut = await CreateSutAsync();

        var state1 = CreateState(Guid.NewGuid().ToString());
        await sut.StoreAsync([state1]);

        state1.Description = "Update";
        await sut.StoreAsync([state1]);

        var found = await sut.FindAsync(state1.InstanceId);

        found.Should().BeEquivalentTo(state1);
    }

    [Fact]
    public async Task Should_query_states()
    {
        var sut = await CreateSutAsync();

        var ownerId = Guid.NewGuid().ToString();
        var state1 = CreateState(ownerId);
        var state2 = CreateState(ownerId);
        await sut.StoreAsync([state1, state2]);

        var found1 = await sut.QueryByOwnerAsync(ownerId);
        found1.Should().BeEquivalentTo((new List<FlowExecutionState<TestFlowContext>> { state1, state2 }, 2));

        var found2 = await sut.QueryByOwnerAsync(ownerId, skip: 1);
        found2.Should().BeEquivalentTo((new List<FlowExecutionState<TestFlowContext>> { state1 }, 2));

        var found3 = await sut.QueryByOwnerAsync(ownerId, take: 1);
        found3.Should().BeEquivalentTo((new List<FlowExecutionState<TestFlowContext>> { state2 }, 2));

        var found4 = await sut.QueryByOwnerAsync(ownerId, definitionId: state2.DefinitionId);
        found4.Should().BeEquivalentTo((new List<FlowExecutionState<TestFlowContext>> { state2 }, 1));
    }

    [Fact]
    public async Task Should_cancel_by_owner_id()
    {
        var sut = await CreateSutAsync();

        var ownerId = Guid.NewGuid().ToString();
        var state1 = CreateState(ownerId);
        var state2 = CreateState(ownerId);

        await sut.StoreAsync([state1, state2]);
        await sut.CancelByOwnerIdAsync(ownerId);

        var found1 = await sut.FindAsync(state1.InstanceId);
        var found2 = await sut.FindAsync(state2.InstanceId);

        Assert.Null(found1!.NextRun);
        Assert.Null(found2!.NextRun);
    }

    [Fact]
    public async Task Should_cancel_by_instance_id()
    {
        var sut = await CreateSutAsync();

        var ownerId = Guid.NewGuid().ToString();
        var state1 = CreateState(ownerId);
        var state2 = CreateState(ownerId);

        await sut.StoreAsync([state1, state2]);
        await sut.CancelByInstanceIdAsync(state1.InstanceId);

        var found1 = await sut.FindAsync(state1.InstanceId);
        var found2 = await sut.FindAsync(state2.InstanceId);

        Assert.Null(found1!.NextRun);
        Assert.NotNull(found2!.NextRun);
    }

    [Fact]
    public async Task Should_cancel_by_definition_id()
    {
        var sut = await CreateSutAsync();

        var ownerId = Guid.NewGuid().ToString();
        var state1 = CreateState(ownerId);
        var state2 = CreateState(ownerId);

        await sut.StoreAsync([state1, state2]);
        await sut.CancelByDefinitionIdAsync(state2.DefinitionId);

        var found1 = await sut.FindAsync(state1.InstanceId);
        var found2 = await sut.FindAsync(state2.InstanceId);

        Assert.NotNull(found1!.NextRun);
        Assert.Null(found2!.NextRun);
    }

    [Fact]
    public async Task Should_delete_by_owner_id()
    {
        var sut = await CreateSutAsync();

        var ownerId1 = Guid.NewGuid().ToString();
        var ownerId2 = Guid.NewGuid().ToString();
        var state1 = CreateState(ownerId1);
        var state2 = CreateState(ownerId2);

        await sut.StoreAsync([state1, state2]);
        await sut.DeleteByOwnerIdAsync(ownerId1);

        var found1 = await sut.FindAsync(state1.InstanceId);
        var found2 = await sut.FindAsync(state2.InstanceId);

        Assert.Null(found1);
        Assert.NotNull(found2);
    }

    [Fact]
    public async Task Should_enqueue()
    {
        var sut = await CreateSutAsync();

        var ownerId = Guid.NewGuid().ToString();
        var state1 = CreateState(ownerId);
        var state2 = CreateState(ownerId);

        var nextRun = Instant.FromDateTimeOffset(new DateTimeOffset(2023, 12, 11, 10, 09, 9, TimeSpan.Zero));

        await sut.StoreAsync([state1, state2]);
        await sut.EnqueueAsync(state2.InstanceId, nextRun);

        var found1 = await sut.FindAsync(state1.InstanceId);
        var found2 = await sut.FindAsync(state2.InstanceId);

        Assert.Equal(FarInFuture, found1!.NextRun!.Value);
        Assert.Equal(nextRun, found2!.NextRun!.Value);
    }

    [Fact]
    public async Task Should_query_pending()
    {
        var sut = await CreateSutAsync();

        var now = Instant.FromDateTimeOffset(new DateTimeOffset(2023, 12, 11, 10, 09, 9, TimeSpan.Zero));

        var ownerId = Guid.NewGuid().ToString();
        var state1 = CreateState(ownerId, 1);
        var state2 = CreateState(ownerId, 2);
        var state3 = CreateState(ownerId, 3);

        state1.NextRun = null;
        state2.NextRun = now;
        state3.NextRun = now.Plus(Duration.FromDays(20));

        await sut.StoreAsync([state1, state2, state3]);

        var nextDay = now.Plus(Duration.FromDays(1));
        var nextMonth = now.Plus(Duration.FromDays(30));

        var found1 = await sut.QueryPendingAsync([1, 2, 3], now).Where(x => x.OwnerId == ownerId).ToListAsync();
        found1.Should().BeEmpty();

        var found2 = await sut.QueryPendingAsync([1, 2, 3], nextDay).Where(x => x.OwnerId == ownerId).ToListAsync();
        found2.Should().BeEquivalentTo([state2]);

        var found3 = await sut.QueryPendingAsync([1, 2, 3], nextMonth).Where(x => x.OwnerId == ownerId).ToListAsync();
        found3.Should().BeEquivalentTo([state2, state3]);

        var found4 = await sut.QueryPendingAsync([3], nextMonth).Where(x => x.OwnerId == ownerId).ToListAsync();
        found4.Should().BeEquivalentTo([state3]);
    }

    private FlowExecutionState<TestFlowContext> CreateState(string ownerId, int partition = 0)
    {
        var time = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromSeconds(counter++));

        return new FlowExecutionState<TestFlowContext>
        {
            SchedulePartition = partition,
            InstanceId = Guid.NewGuid(),
            DefinitionId = Guid.NewGuid().ToString(),
            Definition = null!,
            Completed = time,
            Context = new TestFlowContext(),
            Created = time,
            NextRun = FarInFuture,
            OwnerId = ownerId,
        };
    }
}
