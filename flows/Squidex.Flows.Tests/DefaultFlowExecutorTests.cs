// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using NodaTime;
using Squidex.Flows.Internal;
using Squidex.Flows.Internal.Execution;

namespace Squidex.Flows;

public class DefaultFlowExecutorTests
{
    private readonly IExpressionEngine expressionEngine = A.Fake<IExpressionEngine>();
    private readonly DefaultFlowExecutor<TestFlowContext> sut;
    private Instant now = SystemClock.Instance.GetCurrentInstant();

    public DefaultFlowExecutorTests()
    {
        var clock = A.Fake<IClock>();

        A.CallTo(() => clock.GetCurrentInstant())
            .ReturnsLazily(() => now);

        sut = new DefaultFlowExecutor<TestFlowContext>([],
            new DefaultRetryErrorPolicy<TestFlowContext>(),
            expressionEngine,
            A.Fake<IServiceProvider>(),
            Options.Create(new FlowOptions()))
        {
            Clock = clock,
        };
    }

    [Fact]
    public async Task Should_execute_first_step()
    {
        var stepId1 = Guid.NewGuid();
        var step1 = A.Fake<IFlowStep>();

        A.CallTo(() => step1.ExecuteAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .Returns(new ValueTask<FlowStepResult>(FlowStepResult.Next()));

        var state = sut.CreateState(
            new CreateFlowInstanceRequest<TestFlowContext>
            {
                Context = new TestFlowContext(),
                DefinitionId = Guid.NewGuid().ToString(),
                Definition = new FlowDefinition
                {
                    Steps = new Dictionary<Guid, FlowStepDefinition>
                    {
                        [stepId1] = new FlowStepDefinition { Step = step1 },
                    },
                    InitialStep = stepId1,
                },
                OwnerId = Guid.NewGuid().ToString(),
            });

        await sut.ExecuteAsync(state, default);

        AssertPrepared(step1, 1);
        AssertExecuted(step1, 1);

        await Verify(state).AddNamedGuid(stepId1, "StepId1");
    }

    [Theory]
    [InlineData(ExecutionStatus.Failed)]
    [InlineData(ExecutionStatus.Completed)]
    public async Task Should_execute_nothing_if_already_completed(ExecutionStatus status)
    {
        var stepId1 = Guid.NewGuid();
        var step1 = A.Fake<IFlowStep>();

        A.CallTo(() => step1.ExecuteAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .Returns(new ValueTask<FlowStepResult>(FlowStepResult.Next()));

        var state = sut.CreateState(
            new CreateFlowInstanceRequest<TestFlowContext>
            {
                Context = new TestFlowContext(),
                DefinitionId = Guid.NewGuid().ToString(),
                Definition = new FlowDefinition
                {
                    Steps = new Dictionary<Guid, FlowStepDefinition>
                    {
                        [stepId1] = new FlowStepDefinition { Step = step1 },
                    },
                    InitialStep = stepId1,
                },
                OwnerId = Guid.NewGuid().ToString(),
            });

        state.Status = status;

        await sut.ExecuteAsync(state, default);

        AssertPrepared(step1, 0);
        AssertExecuted(step1, 0);
    }

    [Fact]
    public async Task Should_execute_expression()
    {
        var stepId1 = Guid.NewGuid();
        var step1 = new NoopStepWithExpression { Property = "Expression Source" };

        A.CallTo(() => expressionEngine.RenderAsync("Expression Source", A<TestFlowContext>._, ExpressionFallback.None))
            .Returns("Expression Result");

        var state = sut.CreateState(
            new CreateFlowInstanceRequest<TestFlowContext>
            {
                Context = new TestFlowContext(),
                DefinitionId = Guid.NewGuid().ToString(),
                Definition = new FlowDefinition
                {
                    Steps = new Dictionary<Guid, FlowStepDefinition>
                    {
                        [stepId1] = new FlowStepDefinition { Step = step1 },
                    },
                    InitialStep = stepId1,
                },
                OwnerId = Guid.NewGuid().ToString(),
            });

        await sut.ExecuteAsync(state, default);

        await Verify(state).AddNamedGuid(stepId1, "StepId1");
    }

    [Fact]
    public async Task Should_execute_first_step_with_error()
    {
        var stepId1 = Guid.NewGuid();
        var step1 = A.Fake<IFlowStep>();

        A.CallTo(() => step1.ExecuteAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .Throws(new InvalidOperationException("Step Error"));

        var state = sut.CreateState(
            new CreateFlowInstanceRequest<TestFlowContext>
            {
                Context = new TestFlowContext(),
                DefinitionId = Guid.NewGuid().ToString(),
                Definition = new FlowDefinition
                {
                    Steps = new Dictionary<Guid, FlowStepDefinition>
                    {
                        [stepId1] = new FlowStepDefinition { Step = step1 },
                    },
                    InitialStep = stepId1,
                },
                OwnerId = Guid.NewGuid().ToString(),
            });

        await sut.ExecuteAsync(state, default);

        AssertExecuted(step1, 1);

        await Verify(state).AddNamedGuid(stepId1, "StepId1");
    }

    [Fact]
    public async Task Should_execute_first_step_again_after_error()
    {
        var stepId1 = Guid.NewGuid();
        var step1 = A.Fake<IFlowStep>();

        A.CallTo(() => step1.ExecuteAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .Throws(new InvalidOperationException("Step Error"));

        var state = sut.CreateState(
            new CreateFlowInstanceRequest<TestFlowContext>
            {
                Context = new TestFlowContext(),
                DefinitionId = Guid.NewGuid().ToString(),
                Definition = new FlowDefinition
                {
                    Steps = new Dictionary<Guid, FlowStepDefinition>
                    {
                        [stepId1] = new FlowStepDefinition { Step = step1 },
                    },
                    InitialStep = stepId1,
                },
                OwnerId = Guid.NewGuid().ToString(),
            });

        for (var i = 0; i < 2; i++)
        {
            await sut.ExecuteAsync(state, default);
            now = now.Plus(Duration.FromDays(1));
        }

        AssertPrepared(step1, 1);
        AssertExecuted(step1, 2);

        await Verify(state).AddNamedGuid(stepId1, "StepId1");
    }

    [Fact]
    public async Task Should_execute_first_step_again_after_attempts_exeeded()
    {
        var stepId1 = Guid.NewGuid();
        var step1 = A.Fake<IFlowStep>();

        A.CallTo(() => step1.ExecuteAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .Throws(new InvalidOperationException("Step Error"));

        var state = sut.CreateState(
            new CreateFlowInstanceRequest<TestFlowContext>
            {
                Context = new TestFlowContext(),
                DefinitionId = Guid.NewGuid().ToString(),
                Definition = new FlowDefinition
                {
                    Steps = new Dictionary<Guid, FlowStepDefinition>
                    {
                        [stepId1] = new FlowStepDefinition { Step = step1 },
                    },
                    InitialStep = stepId1,
                },
                OwnerId = Guid.NewGuid().ToString(),
            });

        for (var i = 0; i < 5; i++)
        {
            await sut.ExecuteAsync(state, default);
            now = now.Plus(Duration.FromDays(1));
        }

        AssertPrepared(step1, 1);
        AssertExecuted(step1, 5);

        await Verify(state).AddNamedGuid(stepId1, "StepId1");
    }

    [Fact]
    public async Task Should_execute_next_step_after_success()
    {
        var stepId1 = Guid.Parse("216e4ed4-8e29-4c38-9265-7e5e1f55eb2a");
        var step1 = A.Fake<IFlowStep>();

        var stepId2 = Guid.Parse("216e4ed4-8e29-4c38-9265-7e5e1f55eb2b");
        var step2 = A.Fake<IFlowStep>();

        A.CallTo(() => step1.ExecuteAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .Returns(new ValueTask<FlowStepResult>(FlowStepResult.Next()));

        A.CallTo(() => step2.ExecuteAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .Returns(new ValueTask<FlowStepResult>(FlowStepResult.Next()));

        var state = sut.CreateState(
            new CreateFlowInstanceRequest<TestFlowContext>
            {
                Context = new TestFlowContext(),
                DefinitionId = Guid.NewGuid().ToString(),
                Definition = new FlowDefinition
                {
                    Steps = new Dictionary<Guid, FlowStepDefinition>
                    {
                        [stepId1] = new FlowStepDefinition { Step = step1, NextStepId = stepId2 },
                        [stepId2] = new FlowStepDefinition { Step = step2 },
                    },
                    InitialStep = stepId1,
                },
                OwnerId = Guid.NewGuid().ToString(),
            });

        await sut.ExecuteAsync(state, default);

        AssertPrepared(step1, 1);
        AssertPrepared(step2, 1);

        AssertExecuted(step1, 1);
        AssertExecuted(step2, 1);

        await Verify(state).AddNamedGuid(stepId1, "StepId1").AddNamedGuid(stepId2, "StepId2");
    }

    [Fact]
    public async Task Should_execute_next_step_after_ignored_error()
    {
        var stepId1 = Guid.Parse("216e4ed4-8e29-4c38-9265-7e5e1f55eb2a");
        var step1 = A.Fake<IFlowStep>();

        var stepId2 = Guid.Parse("216e4ed4-8e29-4c38-9265-7e5e1f55eb2b");
        var step2 = A.Fake<IFlowStep>();

        A.CallTo(() => step1.ExecuteAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .Throws(new InvalidOperationException("Step Error"));

        A.CallTo(() => step2.ExecuteAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .Returns(new ValueTask<FlowStepResult>(FlowStepResult.Next()));

        var state = sut.CreateState(
            new CreateFlowInstanceRequest<TestFlowContext>
            {
                Context = new TestFlowContext(),
                DefinitionId = Guid.NewGuid().ToString(),
                Definition = new FlowDefinition
                {
                    Steps = new Dictionary<Guid, FlowStepDefinition>
                    {
                        [stepId1] = new FlowStepDefinition { Step = step1, NextStepId = stepId2, IgnoreError = true },
                        [stepId2] = new FlowStepDefinition { Step = step2 },
                    },
                    InitialStep = stepId1,
                },
                OwnerId = Guid.NewGuid().ToString(),
            });

        await sut.ExecuteAsync(state, default);

        AssertPrepared(step1, 1);
        AssertPrepared(step2, 1);

        AssertExecuted(step1, 1);
        AssertExecuted(step2, 1);

        await Verify(state).AddNamedGuid(stepId1, "StepId1").AddNamedGuid(stepId2, "StepId2");
    }

    [Fact]
    public async Task Should_not_execute_next_step_if_steps_completes_flow()
    {
        var stepId1 = Guid.Parse("216e4ed4-8e29-4c38-9265-7e5e1f55eb2a");
        var step1 = A.Fake<IFlowStep>();

        var stepId2 = Guid.Parse("216e4ed4-8e29-4c38-9265-7e5e1f55eb2b");
        var step2 = A.Fake<IFlowStep>();

        A.CallTo(() => step1.ExecuteAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .Returns(new ValueTask<FlowStepResult>(FlowStepResult.Complete()));

        var state = sut.CreateState(
            new CreateFlowInstanceRequest<TestFlowContext>
            {
                Context = new TestFlowContext(),
                DefinitionId = Guid.NewGuid().ToString(),
                Definition = new FlowDefinition
                {
                    Steps = new Dictionary<Guid, FlowStepDefinition>
                    {
                        [stepId1] = new FlowStepDefinition { Step = step1, NextStepId = stepId2 },
                        [stepId2] = new FlowStepDefinition { Step = step2 },
                    },
                    InitialStep = stepId1,
                },
                OwnerId = Guid.NewGuid().ToString(),
            });

        await sut.ExecuteAsync(state, default);

        AssertPrepared(step1, 1);
        AssertPrepared(step2, 0);

        AssertExecuted(step1, 1);
        AssertExecuted(step2, 0);

        await Verify(state).AddNamedGuid(stepId1, "StepId1").AddNamedGuid(stepId2, "StepId2");
    }

    [Fact]
    public async Task Should_not_execute_next_step_if_steps_jumps_to_specific_step()
    {
        var stepId1 = Guid.Parse("216e4ed4-8e29-4c38-9265-7e5e1f55eb2a");
        var step1 = A.Fake<IFlowStep>();

        var stepId2 = Guid.Parse("216e4ed4-8e29-4c38-9265-7e5e1f55eb2b");
        var step2 = A.Fake<IFlowStep>();

        var stepId3 = Guid.Parse("216e4ed4-8e29-4c38-9265-7e5e1f55eb2c");
        var step3 = A.Fake<IFlowStep>();

        A.CallTo(() => step1.ExecuteAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .Returns(new ValueTask<FlowStepResult>(FlowStepResult.Next(stepId3)));

        A.CallTo(() => step3.ExecuteAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .Returns(new ValueTask<FlowStepResult>(FlowStepResult.Complete()));

        var state = sut.CreateState(
            new CreateFlowInstanceRequest<TestFlowContext>
            {
                Context = new TestFlowContext(),
                DefinitionId = Guid.NewGuid().ToString(),
                Definition = new FlowDefinition
                {
                    Steps = new Dictionary<Guid, FlowStepDefinition>
                    {
                        [stepId1] = new FlowStepDefinition { Step = step1, NextStepId = stepId2 },
                        [stepId2] = new FlowStepDefinition { Step = step2, NextStepId = stepId3 },
                        [stepId3] = new FlowStepDefinition { Step = step3 },
                    },
                    InitialStep = stepId1,
                },
                OwnerId = Guid.NewGuid().ToString(),
            });

        await sut.ExecuteAsync(state, default);

        AssertPrepared(step1, 1);
        AssertPrepared(step2, 0);
        AssertPrepared(step3, 1);

        AssertExecuted(step1, 1);
        AssertExecuted(step2, 0);
        AssertExecuted(step3, 1);

        await Verify(state).AddNamedGuid(stepId1, "StepId1").AddNamedGuid(stepId2, "StepId2").AddNamedGuid(stepId3, "StepId3");
    }

    [Fact]
    public async Task Should_detect_self_executing_step()
    {
        var stepId1 = Guid.NewGuid();
        var step1 = A.Fake<IFlowStep>();

        A.CallTo(() => step1.ExecuteAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .Returns(new ValueTask<FlowStepResult>(FlowStepResult.Next(stepId1)));

        var state = sut.CreateState(
            new CreateFlowInstanceRequest<TestFlowContext>
            {
                Context = new TestFlowContext(),
                DefinitionId = Guid.NewGuid().ToString(),
                Definition = new FlowDefinition
                {
                    Steps = new Dictionary<Guid, FlowStepDefinition>
                    {
                        [stepId1] = new FlowStepDefinition { Step = step1 },
                    },
                    InitialStep = stepId1,
                },
                OwnerId = Guid.NewGuid().ToString(),
            });

        await sut.ExecuteAsync(state, default);

        AssertPrepared(step1, 1);
        AssertExecuted(step1, 1);

        await Verify(state).AddNamedGuid(stepId1, "StepId1");
    }

    [Fact]
    public async Task Should_detect_loop()
    {
        var stepId1 = Guid.Parse("216e4ed4-8e29-4c38-9265-7e5e1f55eb2a");
        var step1 = A.Fake<IFlowStep>();

        var stepId2 = Guid.Parse("216e4ed4-8e29-4c38-9265-7e5e1f55eb2b");
        var step2 = A.Fake<IFlowStep>();

        A.CallTo(() => step1.ExecuteAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .Returns(new ValueTask<FlowStepResult>(FlowStepResult.Next()));

        A.CallTo(() => step2.ExecuteAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .Returns(new ValueTask<FlowStepResult>(FlowStepResult.Next()));

        var state = sut.CreateState(
            new CreateFlowInstanceRequest<TestFlowContext>
            {
                Context = new TestFlowContext(),
                DefinitionId = Guid.NewGuid().ToString(),
                Definition = new FlowDefinition
                {
                    Steps = new Dictionary<Guid, FlowStepDefinition>
                    {
                        [stepId1] = new FlowStepDefinition { Step = step1, NextStepId = stepId2 },
                        [stepId2] = new FlowStepDefinition { Step = step2, NextStepId = stepId1 },
                    },
                    InitialStep = stepId1,
                },
                OwnerId = Guid.NewGuid().ToString(),
            });

        await sut.ExecuteAsync(state, default);

        AssertPrepared(step1, 1);
        AssertExecuted(step1, 1);

        await Verify(state).AddNamedGuid(stepId1, "StepId1").AddNamedGuid(stepId2, "StepId2");
    }

    [Fact]
    public async Task Should_detect_loop_by_result()
    {
        var stepId1 = Guid.Parse("216e4ed4-8e29-4c38-9265-7e5e1f55eb2a");
        var step1 = A.Fake<IFlowStep>();

        var stepId2 = Guid.Parse("216e4ed4-8e29-4c38-9265-7e5e1f55eb2b");
        var step2 = A.Fake<IFlowStep>();

        A.CallTo(() => step1.ExecuteAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .Returns(new ValueTask<FlowStepResult>(FlowStepResult.Next(stepId2)));

        A.CallTo(() => step2.ExecuteAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .Returns(new ValueTask<FlowStepResult>(FlowStepResult.Next(stepId1)));

        var state = sut.CreateState(
            new CreateFlowInstanceRequest<TestFlowContext>
            {
                Context = new TestFlowContext(),
                DefinitionId = Guid.NewGuid().ToString(),
                Definition = new FlowDefinition
                {
                    Steps = new Dictionary<Guid, FlowStepDefinition>
                    {
                        [stepId1] = new FlowStepDefinition { Step = step1 },
                        [stepId2] = new FlowStepDefinition { Step = step2 },
                    },
                    InitialStep = stepId1,
                },
                OwnerId = Guid.NewGuid().ToString(),
            });

        await sut.ExecuteAsync(state, default);

        AssertPrepared(step1, 1);
        AssertExecuted(step1, 1);

        await Verify(state).AddNamedGuid(stepId1, "StepId1").AddNamedGuid(stepId2, "StepId2");
    }

    private static void AssertPrepared(IFlowStep step, int count)
    {
        A.CallTo(() => step.PrepareAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .MustHaveHappenedANumberOfTimesMatching(x => x == count);
    }

    private static void AssertExecuted(IFlowStep step, int count)
    {
        A.CallTo(() => step.ExecuteAsync(A<FlowContext>._, A<FlowExecutionContext>._, A<CancellationToken>._))
            .MustHaveHappenedANumberOfTimesMatching(x => x == count);
    }
}
