// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Squidex.Flows.Internal;
using Squidex.Flows.Internal.Execution;

namespace Squidex.Flows;

public class DefaultFlowExecutor_ValidationTests
{
    private readonly DefaultFlowExecutor<TestFlowContext> sut;

    public DefaultFlowExecutor_ValidationTests()
    {
        sut = new DefaultFlowExecutor<TestFlowContext>(
            A.Fake<IServiceProvider>(),
            [],
            [],
            new NoRetryErrorPolicy<TestFlowContext>(),
            A.Fake<IFlowExpressionEngine>(),
            Options.Create(new FlowOptions()),
            A.Fake<ILogger<DefaultFlowExecutor<TestFlowContext>>>());
    }

    [Fact]
    public async Task Should_error_if_definition_has_no_steps()
    {
        var definition = new FlowDefinition();

        var errors = await ValidateAsync(definition);
        var error = errors.FirstOrDefault();

        Assert.Equal(new Error(string.Empty, ValidationErrorType.NoSteps), error);
    }

    [Fact]
    public async Task Should_error_if_definition_has_no_start_step()
    {
        var definition = new FlowDefinition
        {
            Steps = new Dictionary<Guid, FlowStepDefinition>()
            {
                [Guid.NewGuid()] = new FlowStepDefinition
                {
                    Step = new NoopStep(),
                },
            },
        };

        var errors = await ValidateAsync(definition);
        var error = errors.FirstOrDefault();

        Assert.Equal(new Error(string.Empty, ValidationErrorType.NoStartStep), error);
    }

    [Fact]
    public async Task Should_error_if_step_id_is_invalid()
    {
        var definition = new FlowDefinition
        {
            Steps = new Dictionary<Guid, FlowStepDefinition>()
            {
                [Guid.Empty] = new FlowStepDefinition
                {
                    Step = new NoopStep(),
                },
            },
        };

        var errors = await ValidateAsync(definition);
        var error = errors.LastOrDefault();

        Assert.Equal(new Error(string.Empty, ValidationErrorType.InvalidStepId), error);
    }

    [Fact]
    public async Task Should_error_if_next_step_id_is_invalid()
    {
        var stepId = Guid.NewGuid();

        var definition = new FlowDefinition
        {
            Steps = new Dictionary<Guid, FlowStepDefinition>()
            {
                [stepId] = new FlowStepDefinition
                {
                    Step = new NoopStep(),
                    NextStepId = Guid.NewGuid(),
                },
            },
            InitialStepId = stepId,
        };

        var errors = await ValidateAsync(definition);
        var error = errors.LastOrDefault();

        Assert.Equal(new Error($"steps.{stepId}", ValidationErrorType.InvalidNextStepId), error);
    }

    [Fact]
    public async Task Should_error_if_has_invalid_property()
    {
        var stepId = Guid.NewGuid();

        var definition = new FlowDefinition
        {
            Steps = new Dictionary<Guid, FlowStepDefinition>()
            {
                [stepId] = new FlowStepDefinition
                {
                    Step = new NoopStepWithRequiredProperty(),
                    NextStepId = Guid.NewGuid(),
                },
            },
            InitialStepId = stepId,
        };

        var errors = await ValidateAsync(definition);
        var error = errors.LastOrDefault();

        Assert.Equal(new Error($"steps.{stepId}.Required", ValidationErrorType.InvalidProperty, "The Required field is required."), error);
    }

    [Fact]
    public async Task Should_error_if_has_invalid_custom_property()
    {
        var stepId = Guid.NewGuid();

        var definition = new FlowDefinition
        {
            Steps = new Dictionary<Guid, FlowStepDefinition>()
            {
                [stepId] = new FlowStepDefinition
                {
                    Step = new NoopStepWithCustomValidation(),
                    NextStepId = Guid.NewGuid(),
                },
            },
            InitialStepId = stepId,
        };

        var errors = await ValidateAsync(definition);
        var error = errors.LastOrDefault();

        Assert.Equal(new Error($"steps.{stepId}.Custom", ValidationErrorType.InvalidProperty, "The Custom field has validation rules."), error);
    }

    [Fact]
    public async Task Should_not_error_is_is_valid()
    {
        var stepId1 = Guid.Parse("216e4ed4-8e29-4c38-9265-7e5e1f55eb2a");
        var stepId2 = Guid.NewGuid();

        var definition = new FlowDefinition
        {
            Steps = new Dictionary<Guid, FlowStepDefinition>()
            {
                [stepId1] = new FlowStepDefinition
                {
                    Step = new NoopStepWithRequiredProperty { Required = "Step1" },
                    NextStepId = stepId2,
                },
                [stepId2] = new FlowStepDefinition
                {
                    Step = new NoopStepWithRequiredProperty { Required = "Step2" },
                    NextStepId = default,
                },
            },
            InitialStepId = stepId1,
        };

        var errors = await ValidateAsync(definition);

        Assert.Empty(errors);
    }

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
#pragma warning disable RECS0082 // Parameter has the same name as a member and hides it
    private record struct Error(string Path, ValidationErrorType Type, string Message = "");
#pragma warning restore RECS0082 // Parameter has the same name as a member and hides it
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

    private async Task<List<Error>> ValidateAsync(FlowDefinition definition)
    {
        var errors = new List<Error>();

        await sut.ValidateAsync(definition,
            (path, type, message) => errors.Add(new Error(path, type, message)),
            default);

        return errors;
    }
}
