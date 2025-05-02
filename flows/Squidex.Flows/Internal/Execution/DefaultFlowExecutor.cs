// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Extensions;
using Squidex.Text;

namespace Squidex.Flows.Internal.Execution;

public sealed class DefaultFlowExecutor<TContext>(
    IServiceProvider serviceProvider,
    IEnumerable<IFlowMiddleware> middlewares,
    IEnumerable<IFlowExecutionCallback<TContext>> callbacks,
    IFlowErrorPolicy<TContext> errorPolicy,
    IFlowExpressionEngine expressionEngine,
    IOptions<FlowOptions> flowOptions,
    ILogger<DefaultFlowExecutor<TContext>> log)
    : IFlowExecutor<TContext> where TContext : FlowContext
{
    private readonly PipelineDelegate pipeline = BuildPipeline(middlewares);

    private TimeSpan DefaultTimeout => flowOptions.Value.DefaultTimeout;

    public IClock Clock { get; set; } = SystemClock.Instance;

    public async Task ValidateAsync(FlowDefinition definition, AddError addError,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(nameof(definition));
        ArgumentNullException.ThrowIfNull(nameof(addError));

        if (definition.Steps.Count == 0)
        {
            addError(string.Empty, ValidationErrorType.NoSteps);
            return;
        }

        if (definition.InitialStep == default || !definition.Steps.ContainsKey(definition.InitialStep))
        {
            addError(string.Empty, ValidationErrorType.NoStartStep);
        }

        var context = new FlowValidationContext(serviceProvider, definition);

        foreach (var (stepId, stepDefinition) in definition.Steps)
        {
            if (stepId == default)
            {
                addError(string.Empty, ValidationErrorType.InvalidStepId);
            }

            if (stepDefinition.NextStepId != null &&
                stepDefinition.NextStepId != default &&
                !definition.Steps.ContainsKey(stepDefinition.NextStepId.Value))
            {
                addError($"steps.{stepId}", ValidationErrorType.InvalidNextStepId);
            }

            ValidateProperties(addError, stepId, stepDefinition.Step);

            await stepDefinition.Step.ValidateAsync(context, (path, message) =>
            {
                addError($"steps.{stepId}.{path}", ValidationErrorType.InvalidProperty, message);
            }, ct);
        }
    }

    private static void ValidateProperties(AddError addError, Guid stepId, FlowStep step)
    {
        var context = new ValidationContext(step);
        var errors = new List<ValidationResult>();

        if (Validator.TryValidateObject(step, context, errors, true))
        {
            return;
        }

        foreach (var error in errors)
        {
            if (string.IsNullOrWhiteSpace(error.ErrorMessage))
            {
                continue;
            }

            foreach (var member in error.MemberNames)
            {
                addError($"steps.{stepId}.{member}", ValidationErrorType.InvalidProperty, error.ErrorMessage);
            }
        }
    }

    public FlowExecutionState<TContext> CreateState(CreateFlowInstanceRequest<TContext> request)
    {
        ArgumentNullException.ThrowIfNull(nameof(request.Definition));
        ArgumentNullException.ThrowIfNull(nameof(request.Context));
        ArgumentNullException.ThrowIfNull(nameof(request.ScheduleKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(nameof(request.OwnerId));
        ArgumentException.ThrowIfNullOrWhiteSpace(nameof(request.DefinitionId));
        ArgumentException.ThrowIfNullOrWhiteSpace(nameof(request.Description));
        ArgumentException.ThrowIfNullOrWhiteSpace(nameof(request.ScheduleKey));

        if (request.Definition.Steps.Count == 0)
        {
            throw new InvalidOperationException($"Flow definition has no steps.");
        }

        if (!request.Definition.Steps.ContainsKey(request.Definition.InitialStep))
        {
            throw new InvalidOperationException($"Flow definition has no step with ID '{request.Definition.InitialStep}'.");
        }

        var scheduleKey = request.ScheduleKey ?? string.Empty;
        var scheduleHash = Math.Abs(scheduleKey.GetDeterministicHashCode());
        var schedulePartition = scheduleHash % flowOptions.Value.NumPartitions;

        var state = new FlowExecutionState<TContext>
        {
            Created = Clock.GetCurrentInstant(),
            Context = request.Context,
            Definition = request.Definition,
            DefinitionId = request.DefinitionId,
            Description = request.Description ?? string.Empty,
            Expires = Clock.GetCurrentInstant().Plus(flowOptions.Value.Expiration.ToDuration()),
            InstanceId = Guid.NewGuid(),
            NextRun = Clock.GetCurrentInstant(),
            NextStepId = request.Definition.InitialStep,
            OwnerId = request.OwnerId,
            ScheduleKey = scheduleKey,
            SchedulePartition = schedulePartition,
        };

        return state;
    }

    public async Task SimulateAsync(FlowExecutionState<TContext> state,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(nameof(state));

        while (true)
        {
            if (state.Status is FlowExecutionStatus.Completed or FlowExecutionStatus.Failed)
            {
                break;
            }

            await ExecuteCoreAsync(state, true, default, ct);
        }
    }

    public async Task ExecuteAsync(FlowExecutionState<TContext> state,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(nameof(state));

        while (true)
        {
            if (state.Status is FlowExecutionStatus.Completed or FlowExecutionStatus.Failed)
            {
                break;
            }

            var now = Clock.GetCurrentInstant();
            if (state.NextRun > now)
            {
                break;
            }

            await ExecuteCoreAsync(state, false, DefaultTimeout, ct);
        }
    }

    private async Task ExecuteCoreAsync(FlowExecutionState<TContext> state, bool simulate, TimeSpan timeout,
        CancellationToken ct)
    {
        var definition = state.Definition;

        var stepId = state.NextStepId ?? Guid.Empty;
        if (stepId == default)
        {
            throw new InvalidOperationException("Flow has not next step.");
        }

        if (!definition.Steps.TryGetValue(stepId, out var stepDefinition))
        {
            throw new InvalidOperationException($"Cannot find step with ID '{definition.InitialStep}'.");
        }

        if (state.Status == FlowExecutionStatus.Scheduled)
        {
            state.Status = FlowExecutionStatus.Running;
        }

        var stepState = state.Step(stepId);
        var stepAttempt = stepState.NextAttempt(Clock.GetCurrentInstant());

        void Log(string message, string? dump)
        {
            lock (stepAttempt.Log)
            {
                stepAttempt.Log.Add(new FlowExecutionStepLogEntry(Clock.GetCurrentInstant(), message, dump));
            }
        }

        var executionContext = new FlowExecutionContext(
            expressionEngine,
            stepDefinition.Step,
            state.Context,
            serviceProvider,
            Log,
            simulate);

        using (var cts = CreateCancellationTokenSource(timeout, ct))
        {
            // Detect circular references to other steps.
            if (stepState.Status is FlowExecutionStatus.Completed or FlowExecutionStatus.Failed)
            {
                throw new InvalidOperationException("Flow step has already been completed.");
            }

            await PrepareAsync(executionContext, stepDefinition.Step, stepState, cts.Token);
            try
            {
                stepState.Status = FlowExecutionStatus.Running;

                var result = await pipeline(executionContext, ct) ??
                    throw new InvalidOperationException("Step does not return a valid result.");

                HandleSuccess(state, stepDefinition, stepState, result);
            }
            catch (Exception ex)
            {
                HandleError(state, stepId, stepDefinition, stepState, stepAttempt, ex);
            }
            finally
            {
                stepAttempt.Completed = Clock.GetCurrentInstant();
            }

            await HandleCallbacksAsync(state, ct);
        }
    }

    private async Task HandleCallbacksAsync(FlowExecutionState<TContext> state,
        CancellationToken ct)
    {
        foreach (var callback in callbacks)
        {
            try
            {
                await callback.OnUpdateAsync(state, ct);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to execute {callback}.", callback);
            }
        }
    }

    private void HandleError(
        FlowExecutionState<TContext> state,
        Guid stepId,
        FlowStepDefinition stepDefinition,
        FlowExecutionStepState stepState,
        FlowExecutionStepAttempt stepAttempt,
        Exception ex)
    {
        // Ensure to take the time after the step execution and preparation.
        var now = Clock.GetCurrentInstant();

        if (stepDefinition.IgnoreError)
        {
            var nextId = state.GetNextStep(stepDefinition, default);
            if (nextId != null)
            {
                state.Next(nextId.Value, Instant.MinValue);
            }
            else
            {
                state.Complete(now);
            }
        }
        else
        {
            var nextAttempt = errorPolicy.ShouldRetry(state, stepState, stepDefinition.Step, now);
            if (nextAttempt > now)
            {
                state.Next(stepId, nextAttempt.Value);
            }
            else
            {
                state.Failed(now);
                stepState.Status = FlowExecutionStatus.Failed;
            }
        }

        if (flowOptions.Value.IsSafeException?.Invoke(ex) == true)
        {
            stepAttempt.Error = ex.Message;
        }
    }

    private void HandleSuccess(
        FlowExecutionState<TContext> state,
        FlowStepDefinition stepDefinition,
        FlowExecutionStepState stepState,
        FlowStepResult result)
    {
        // Ensure to take the time after the step execution and preparation.
        var now = Clock.GetCurrentInstant();

        // This step has been successful (we do not support loops).
        stepState.Status = FlowExecutionStatus.Completed;

        if (result.Type == FlowStepResultType.Next)
        {
            var nextId = state.GetNextStep(stepDefinition, result.StepId);
            if (nextId != null)
            {
                state.Next(nextId.Value, result.Scheduled);
                return;
            }
        }

        state.Complete(now);
    }

    private static CancellationTokenSource CreateCancellationTokenSource(TimeSpan timeout, CancellationToken ct)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        if (timeout > TimeSpan.Zero)
        {
            cts.CancelAfter(timeout);
        }

        return cts;
    }

    private static async Task PrepareAsync(
        FlowExecutionContext executionContext,
        FlowStep step,
        FlowExecutionStepState stepState,
        CancellationToken ct)
    {
        if (stepState.IsPrepared)
        {
            return;
        }

        await step.EvaluateExpressionsAsync(executionContext);
        await step.PrepareAsync(executionContext, ct);
        stepState.IsPrepared = true;
    }

    private static PipelineDelegate BuildPipeline(IEnumerable<IFlowMiddleware> middlewares)
    {
        return new PipelineDelegate((executionContext, ct) =>
        {
            NextStepDelegate next = () =>
            {
                return executionContext.Step.ExecuteAsync(executionContext, ct);
            };

            foreach (var middleware in middlewares.Reverse())
            {
                var currentNext = next;
                next = () =>
                {
                    return middleware.InvokeAsync(executionContext, currentNext, ct);
                };
            }

            return next();
        });
    }
}
