// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using Squidex.Flows.Internal.Execution.Utils;
using Squidex.Hosting;

namespace Squidex.Flows.Internal.Execution;

public sealed class FlowExecutionWorker<TContext> : BackgroundService, IBackgroundProcess where TContext : FlowContext
{
    private readonly ConcurrentDictionary<Guid, bool> executing = new ConcurrentDictionary<Guid, bool>();
    private readonly PartitionedScheduler<FlowExecutionState<TContext>> requestScheduler;
    private readonly FlowOptions options;
    private readonly IFlowExecutor<TContext> executor;
    private readonly IFlowStateStore<TContext> store;
    private readonly ILogger<FlowExecutionWorker<TContext>> log;
    private readonly int[] partitions;

    public IClock Clock { get; set; } = SystemClock.Instance;

    public FlowExecutionWorker(
        IFlowExecutor<TContext> executor,
        IFlowStateStore<TContext> store,
        IOptions<FlowOptions> options,
        ILogger<FlowExecutionWorker<TContext>> log)
    {
        this.executor = executor;
        this.options = options.Value;
        partitions = options.Value.GetPartitions();
        this.store = store;
        this.log = log;

        requestScheduler = new PartitionedScheduler<FlowExecutionState<TContext>>(HandleAsync, 32, 2);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(options.JobQueryInterval);
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
            await foreach (var next in store.QueryPendingAsync(partitions, now, ct))
            {
                await requestScheduler.ScheduleAsync(next.ScheduleKey, next, ct);
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

        try
        {
            await executor.ExecuteAsync(state, ct);
            await store.StoreAsync([state], default);
        }
        finally
        {
            executing.Remove(state.InstanceId, out _);
        }
    }
}
