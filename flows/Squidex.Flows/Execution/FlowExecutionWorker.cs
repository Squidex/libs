// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using Squidex.Flows.Execution.Utils;

namespace Squidex.Flows.Execution;

public sealed class FlowExecutionWorker<TContext> : BackgroundService where TContext : FlowContext
{
    private readonly ConcurrentDictionary<Guid, bool> executing = new ConcurrentDictionary<Guid, bool>();
    private readonly PartitionedScheduler<FlowExecutionState<TContext>> requestScheduler;
    private readonly IFlowExecutor<TContext> executor;
    private readonly IFlowStateStore<TContext> store;
    private readonly ILogger<FlowExecutionWorker<TContext>> log;

    public IClock Clock { get; set; } = SystemClock.Instance;

    public FlowExecutionWorker(
        IFlowExecutor<TContext> executor,
        IFlowStateStore<TContext> store,
        ILogger<FlowExecutionWorker<TContext>> log)
    {
        this.executor = executor;
        this.store = store;
        this.log = log;

        requestScheduler = new PartitionedScheduler<FlowExecutionState<TContext>>(HandleAsync, 32, 2);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(10));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await QueryAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async Task QueryAsync(
        CancellationToken ct)
    {
        try
        {
            var now = Clock.GetCurrentInstant();
            await foreach (var next in store.QueryPendingAsync(now, ct))
            {
                await requestScheduler.ScheduleAsync(next.ExecutionPartition, next, ct);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to query rule events.");
        }
    }

    public async Task HandleAsync(FlowExecutionState<TContext> state,
        CancellationToken ct)
    {
        if (!executing.TryAdd(state.InstanceId, false))
        {
            return;
        }

        await executor.ExecuteAsync(state, default, ct);
        await store.StoreAsync([state], default);
    }
}
