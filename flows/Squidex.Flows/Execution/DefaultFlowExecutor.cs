﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using NodaTime;
using Squidex.Flows.Internal;

namespace Squidex.Flows.Execution;

public sealed class DefaultFlowExecutor<TContext>(
    IEnumerable<IFlowMiddleware> middlewares,
    IErrorPolicy<TContext> errorPolicy,
    IExpressionEngine expressionEngine,
    IServiceProvider serviceProvider)
    : IFlowExecutor<TContext> where TContext : FlowContext
{
    private readonly List<IFlowMiddleware> reverseMiddlewares = middlewares.Reverse().ToList();

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

        var context = new FlowValidationContext(serviceProvider, definition);

        foreach (var (stepId, stepDefinition) in definition.Steps)
        {
            if (stepId == default)
            {
                addError(string.Empty, ValidationErrorType.InvalidStepId);
            }

            ValidateProperties(addError, stepId, stepDefinition.Step);

            await stepDefinition.Step.ValidateAsync(context, addError, ct);
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

    public Task<FlowExecutionState<TContext>> CreateInstanceAsync(
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

        var state = new FlowExecutionState<TContext>
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

    public async Task SimulateAsync(FlowExecutionState<TContext> state,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(nameof(state));

        var options = new ExecutionOptions { IsSimulation = true };

        while (true)
        {
            if (state.Status is ExecutionStatus.Completed or ExecutionStatus.Failed)
            {
                break;
            }

            await ExecuteAsync(state, options, ct);
        }
    }

    public async Task ExecuteAsync(FlowExecutionState<TContext> state, ExecutionOptions options,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(nameof(state));

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

    private async Task ExecuteCoreAsync(FlowExecutionState<TContext> state, ExecutionOptions options, IClock clock,
        CancellationToken ct)
    {
        var definition = state.Definition;

        var stepId = state.NextStep;
        if (stepId == default)
        {
            throw new InvalidOperationException("Flow has not next step.");
        }

        if (!definition.Steps.TryGetValue(stepId, out var stepDefinition))
        {
            throw new InvalidOperationException($"Cannot find step with ID '{definition.InitialStep}'.");
        }

        var step = stepDefinition.Step;

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

        void Log(string message, string? dump)
        {
            lock (stepAttempt.Log)
            {
                stepAttempt.Log.Add(new ExecutionStepLogEntry(Clock.GetCurrentInstant(), message, dump));
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

            await PrepareAsync(state, step, stepState, executionContext, combined.Token);
            try
            {
                stepState.Status = ExecutionStatus.Running;

                var pipeline = BuildPipeline(state.Context, executionContext, step, ct);

                // The pipeline is built for each execution, as the costs are acceptable.
                var result = await pipeline();

                // Ensure to take the time after the step execution and preparation.
                var now = clock.GetCurrentInstant();

                // This step has been successful (we do not support loops).
                stepState.Status = ExecutionStatus.Completed;

                if (result.Type == FlowStepResultType.Next)
                {
                    var nextId = GetNextStep(state, stepDefinition, result);
                    if (nextId != default)
                    {
                        state.Next(nextId, result.Scheduled);
                        return;
                    }
                }

                state.Complete(now);
            }
            catch (Exception ex)
            {
                // Ensure to take the time after the step execution and preparation.
                var now = clock.GetCurrentInstant();

                if (stepDefinition.IgnoreError)
                {
                    var nextId = GetNextStep(state, stepDefinition, FlowStepResult.Next());
                    if (nextId != default)
                    {
                        state.Next(stepId, Instant.MinValue);
                    }
                    else
                    {
                        state.Complete(now);
                    }
                }
                else
                {
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
                }

                stepAttempt.Error = ex;
            }
            finally
            {
                stepAttempt.Completed = clock.GetCurrentInstant();
            }
        }
    }

    private static async Task PrepareAsync(FlowExecutionState<TContext> state, IFlowStep step, ExecutionStepState stepState, FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        if (stepState.IsPrepared)
        {
            return;
        }

        foreach (var property in step.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.PropertyType != typeof(string))
            {
                continue;
            }

            if (!property.CanWrite || !property.CanRead)
            {
                continue;
            }

            var attribute = property.GetCustomAttribute<ExpressionAttribute>();
            if (attribute == null)
            {
                continue;
            }

            var expressionSource = property.GetValue(step, null) as string;
            var expressionResult = await executionContext.RenderAsync(expressionSource, state.Complete, attribute.Fallback);

            property.SetValue(step, expressionResult, null);
        }

        await step.PrepareAsync(state.Context, executionContext, ct);
        stepState.IsPrepared = true;
    }

    private NextStepDelegate BuildPipeline(TContext context, FlowExecutionContext executionContext, IFlowStep step,
        CancellationToken ct)
    {
        NextStepDelegate next = () =>
        {
            return step.ExecuteAsync(context, executionContext, ct);
        };

        foreach (var middleware in reverseMiddlewares)
        {
            var currentNext = next;
            next = () =>
            {
                return middleware.InvokeAsync(context, executionContext, step, currentNext, ct);
            };
        }

        return next;
    }

    private static Guid GetNextStep(FlowExecutionState<TContext> state, FlowStepDefinition currentStep, FlowStepResult result)
    {
        var nextId = result.StepId;
        if (nextId == default)
        {
            nextId = currentStep.NextStepId;
        }

        if (nextId == default || !state.Definition.Steps.ContainsKey(nextId))
        {
            return default;
        }

        if (state.Step(nextId).Status != ExecutionStatus.Pending)
        {
            return default;
        }

        return nextId;
    }
}
