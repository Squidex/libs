// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel.DataAnnotations;
using NodaTime;
using Squidex.Flows.Internal;

namespace Squidex.Flows.Execution;

internal sealed class DefaultFlowExecutor<TContext>(
    IErrorPolicy<TContext> errorPolicy,
    IExpressionEngine expressionEngine,
    IServiceProvider serviceProvider)
    : IFlowExecutor<TContext> where TContext : FlowContext
{
    public IClock Clock { get; set; } = SystemClock.Instance;

    public async Task ValidateAsync(FlowDefinition definition, AddError addError,
        CancellationToken ct)
    {
        if (definition.Steps.Count == 0)
        {
            addError(string.Empty, ValidationErrorType.NoSteps);
        }
        else if (definition.InitialStep == default || !definition.Steps.ContainsKey(definition.InitialStep))
        {
            addError(string.Empty, ValidationErrorType.NoStartStep);
        }

        foreach (var (stepId, stepDefinition) in definition.Steps)
        {
            if (stepId == default)
            {
                addError(string.Empty, ValidationErrorType.InvalidStepId);
            }

            ValidateProperties(addError, stepId, stepDefinition.Step);

            await stepDefinition.Step.ValidateAsync(definition, addError, ct);
        }
    }

    private static void ValidateProperties(AddError addError, Guid stepId, IFlowStep step)
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

    public Task<ExecutionState<TContext>> CreateInstanceAsync(
        string ownerId,
        string definitionId,
        string description,
        FlowDefinition definition,
        TContext context,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameof(ownerId));
        ArgumentException.ThrowIfNullOrWhiteSpace(nameof(definitionId));
        ArgumentException.ThrowIfNullOrWhiteSpace(nameof(description));
        ArgumentNullException.ThrowIfNull(nameof(definition));
        ArgumentNullException.ThrowIfNull(nameof(context));

        if (definition.Steps.Count == 0)
        {
            throw new InvalidOperationException($"Flow definition has no steps.");
        }

        if (!definition.Steps.ContainsKey(definition.InitialStep))
        {
            throw new InvalidOperationException($"Flow definition has no step with ID '{definition.InitialStep}'.");
        }

        var state = new ExecutionState<TContext>
        {
            Context = context,
            Definition = definition,
            DefinitionId = definitionId,
            Description = description,
            InstanceId = Guid.NewGuid(),
            NextRun = Clock.GetCurrentInstant(),
            NextStep = definition.InitialStep,
            OwnerId = ownerId
        };

        return Task.FromResult(state);
    }

    public async Task SimulateAsync(FlowDefinition definition, TContext context,
        CancellationToken ct)
    {
        var options = new ExecutionOptions { IsSimulation = true };

        var state = new ExecutionState<TContext>
        {
            Context = context,
            Definition = definition,
            DefinitionId = Guid.NewGuid().ToString(),
            Description = string.Empty,
            InstanceId = Guid.NewGuid(),
            NextRun = Clock.GetCurrentInstant(),
            NextStep = definition.InitialStep,
            OwnerId = string.Empty
        };

        while (true)
        {
            if (state.Status is ExecutionStatus.Completed or ExecutionStatus.Failed)
            {
                break;
            }

            await ExecuteAsync(state, options, ct);
        }
    }

    public async Task ExecuteAsync(ExecutionState<TContext> state, ExecutionOptions options,
        CancellationToken ct)
    {
        while (true)
        {
            if (state.Status is ExecutionStatus.Completed or ExecutionStatus.Failed)
            {
                break;
            }

            var now = Clock.GetCurrentInstant();
            if (state.NextRun > now)
            {
                break;
            }

            await ExecuteCoreAsync(state, options, Clock, ct);
        }
    }

    private async Task ExecuteCoreAsync(ExecutionState<TContext> state, ExecutionOptions options, IClock clock,
        CancellationToken ct)
    {
        var definition = state.Definition;

        var stepId = state.NextStep;
        if (stepId == default)
        {
            throw new InvalidOperationException("Flow has not next step.");
        }

        if (!definition.Steps.TryGetValue(definition.InitialStep, out var stepDefinition))
        {
            throw new InvalidOperationException($"Cannot find step with ID '{definition.InitialStep}'.");
        }

        if (stepDefinition.Step is not IFlowStep step)
        {
            throw new InvalidOperationException($"Step has an invalid type.");
        }

        if (state.Status is ExecutionStatus.Completed or ExecutionStatus.Failed)
        {
            throw new InvalidOperationException("Flow has already been completed.");
        }

        if (state.Status == ExecutionStatus.Scheduled)
        {
            state.Status = ExecutionStatus.Running;
        }

        var stepState = state.Step(stepId);
        var stepAttempt = stepState.NextAttempt(clock.GetCurrentInstant());

        void Log(string message)
        {
            lock (stepAttempt.Log)
            {
                stepAttempt.Log.Add(new ExecutionStepLogEntry(Clock.GetCurrentInstant(), message));
            }
        }

        var executionContext = new FlowExecutionContext(expressionEngine, serviceProvider, Log, options.IsSimulation);
        using (var combined = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            // Enforce a timeout after a configured time span.
            combined.CancelAfter(options.Timeout);

            // Detect circular references to other steps.
            if (stepState.Status is ExecutionStatus.Completed or ExecutionStatus.Failed)
            {
                throw new InvalidOperationException("Flow step has already been completed.");
            }

            if (!stepState.IsPrepared)
            {
                await step.PrepareAsync(state.Context, executionContext!, combined.Token);
            }

            try
            {
                stepState.Status = ExecutionStatus.Running;

                FlowStepResult result;
                FlowConsole.Output = Log;
                try
                {
                    result = await step.ExecuteAsync(state.Context, executionContext, combined.Token);
                }
                finally
                {
                    FlowConsole.Output = null;
                }

                // Ensure to take the time after the step execution and preparation.
                var now = clock.GetCurrentInstant();

                // This step has been successful (we do not support loops).
                stepState.Status = ExecutionStatus.Completed;

                if (result.Type == FlowStepResultType.Next)
                {
                    var nextId = GetNextStep(state, stepDefinition, result);
                    if (nextId != null)
                    {
                        state.Next(nextId.Value, Instant.MinValue);
                        return;
                    }
                }

                state.Complete(now);
            }
            catch (Exception ex)
            {
                // Ensure to take the time after the step execution and preparation.
                var now = clock.GetCurrentInstant();

                var nextAttempt = errorPolicy.ShouldRetry(state, stepState, step);
                if (nextAttempt > now)
                {
                    state.Next(stepId, nextAttempt.Value);
                }
                else
                {
                    state.Failed(now);
                    stepState.Status = ExecutionStatus.Failed;
                }

                stepAttempt.Error = ex;
            }
        }
    }

    private static Guid? GetNextStep(ExecutionState<TContext> state, FlowStepDefinition currentStep, FlowStepResult result)
    {
        var nextId = result.StepId ?? currentStep.NextStepId;
        if (!state.Definition.Steps.ContainsKey(nextId))
        {
            return null;
        }

        if (state.Step(nextId).Status != ExecutionStatus.Pending)
        {
            return null;
        }

        return nextId;
    }
}
