// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using NodaTime;
using Squidex.Flows.Internal;
using Squidex.Flows.Internal.Execution;

namespace Squidex.Flows;

public class DefaultFlowManagerTests
{
    private readonly IFlowStateStore<TestFlowContext> flowStateStore = A.Fake<IFlowStateStore<TestFlowContext>>();
    private readonly IFlowExecutor<TestFlowContext> flowExecutor = A.Fake<IFlowExecutor<TestFlowContext>>();
    private readonly CancellationTokenSource cts = new CancellationTokenSource();
    private readonly CancellationToken ct;
    private readonly DefaultFlowManager<TestFlowContext> sut;

    public DefaultFlowManagerTests()
    {
        ct = cts.Token;

        sut = new DefaultFlowManager<TestFlowContext>(flowStateStore, flowExecutor);
    }

    [Fact]
    public async Task Should_forward_created_instances_to_store()
    {
        var id = Guid.NewGuid();

        var state1 = CreateState();
        var state2 = CreateState();

        var request1 = new CreateFlowInstanceRequest<TestFlowContext>
        {
            Context = state1.Context,
            Definition = state1.Definition,
            DefinitionId = state1.DefinitionId,
            Description = state1.Description,
            OwnerId = state1.OwnerId,
            ScheduleKey = state1.ScheduleKey,
        };

        var request2 = new CreateFlowInstanceRequest<TestFlowContext>
        {
            Context = state2.Context,
            Definition = state2.Definition,
            DefinitionId = state2.DefinitionId,
            Description = state2.Description,
            OwnerId = state2.OwnerId,
            ScheduleKey = state2.ScheduleKey,
        };

        A.CallTo(() => flowExecutor.CreateState(request1))
            .Returns(state1);

        A.CallTo(() => flowExecutor.CreateState(request2))
            .Returns(state2);

        await sut.EnqueueAsync([request1, request2], ct);

        A.CallTo(() => flowStateStore.StoreAsync(
                A<List<FlowExecutionState<TestFlowContext>>>.That.Matches(items =>
                    items.Count == 2 &&
                    items[0].Equals(state1) &&
                    items[1].Equals(state2)),
                ct))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Should_forward_cancel_by_instance_id_to_store()
    {
        var id = Guid.NewGuid();

        await sut.CancelByInstanceIdAsync(id, ct);

        A.CallTo(() => flowStateStore.CancelByInstanceIdAsync(id, ct))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Should_forward_cancel_by_definition_id_to_store()
    {
        var id = Guid.NewGuid().ToString();

        await sut.CancelByDefinitionIdAsync(id, ct);

        A.CallTo(() => flowStateStore.CancelByDefinitionIdAsync(id, ct))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Should_forward_cancel_by_owner_id_to_store()
    {
        var id = Guid.NewGuid().ToString();

        await sut.CancelByOwnerIdAsync(id, ct);

        A.CallTo(() => flowStateStore.CancelByOwnerIdAsync(id, ct))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Should_forward_delete_by_owner_id_to_store()
    {
        var id = Guid.NewGuid().ToString();

        await sut.DeleteByOwnerIdAsync(id, ct);

        A.CallTo(() => flowStateStore.DeleteByOwnerIdAsync(id, ct))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Should_forward_query_to_store()
    {
        var ownerId = Guid.NewGuid().ToString();
        var pageOffset = 14;
        var pageSize = 12;
        var definitionid = Guid.NewGuid().ToString();

        var items = new List<FlowExecutionState<TestFlowContext>>
        {
            CreateState(),
            CreateState(),
        };

        A.CallTo(() => flowStateStore.QueryByOwnerAsync(ownerId, definitionid, pageOffset, pageSize, ct))
            .Returns((items, 42));

        var (result, total) = await sut.QueryInstancesByOwnerAsync(ownerId, definitionid, pageOffset, pageSize, ct);

        Assert.Equal(42, total);
        Assert.Same(items, result);
    }

    [Fact]
    public async Task Should_query_states_and_mark_them_as_cancelled_if_no_nextrun_is_given()
    {
        var ownerId = Guid.NewGuid().ToString();
        var pageOffset = 14;
        var pageSize = 12;
        var definitionid = Guid.NewGuid().ToString();

        var items = new List<FlowExecutionState<TestFlowContext>>
        {
            CreateState(nextRun: true),
            CreateState(nextRun: false),
        };

        A.CallTo(() => flowStateStore.QueryByOwnerAsync(ownerId, definitionid, pageOffset, pageSize, ct))
            .Returns((items, 42));

        var (result, total) = await sut.QueryInstancesByOwnerAsync(ownerId, definitionid, pageOffset, pageSize, ct);

        Assert.Equal(42, total);
        Assert.Equal(FlowExecutionStatus.Pending, result[0].Status);
        Assert.Equal(FlowExecutionStatus.Cancelled, result[1].Status);
    }

    [Fact]
    public async Task Should_forward_simulation_to_executor()
    {
        await sut.SimulateAsync(default, ct);

        A.CallTo(() => flowExecutor.SimulateAsync(A<FlowExecutionState<TestFlowContext>>._, ct))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Should_forward_flow_validation_to_executor()
    {
        await sut.ValidateAsync((FlowDefinition)null!, null!, ct);

        A.CallTo(() => flowExecutor.ValidateAsync((FlowDefinition)null!, null!, ct))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Should_forward_step_validation_to_executor()
    {
        await sut.ValidateAsync((FlowStep)null!, null!, ct);

        A.CallTo(() => flowExecutor.ValidateAsync((FlowStep)null!, null!, ct))
            .MustHaveHappened();
    }

    private static FlowExecutionState<TestFlowContext> CreateState(bool nextRun = true)
    {
        return new FlowExecutionState<TestFlowContext>
        {
            InstanceId = Guid.NewGuid(),
            DefinitionId = Guid.NewGuid().ToString(),
            Definition = null!,
            Context = new TestFlowContext(),
            OwnerId = Guid.NewGuid().ToString(),
            NextRun = nextRun ? SystemClock.Instance.GetCurrentInstant() : null,
            NextStepId = null,
            Steps = new Dictionary<Guid, FlowExecutionStepState>
            {
                [Guid.NewGuid()] = new FlowExecutionStepState()
            }
        };
    }
}
