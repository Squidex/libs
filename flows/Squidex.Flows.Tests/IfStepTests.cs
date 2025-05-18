// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Flows.Internal;
using Squidex.Flows.Internal.Execution;
using Squidex.Flows.Steps;

namespace Squidex.Flows;

public class IfStepTests
{
    private readonly IFlowExpressionEngine expressionEngine = A.Fake<IFlowExpressionEngine>();
    private readonly FlowExecutionContext executionContext;

    public IfStepTests()
    {
        executionContext = new FlowExecutionContext(expressionEngine, null!, null!, null!, (_, _) => { }, false);
    }

    [Fact]
    public async Task Should_add_error_if_branch_has_invalid_target()
    {
        var @if = new IfFlowStep
        {
            Branches =
            [
                new IfFlowBranch { NextStepId = Guid.NewGuid() },
            ],
        };

        var errors = await ValidateAsync(@if, new FlowDefinition());

        Assert.NotEmpty(errors);
    }

    [Fact]
    public async Task Should_add_error_if_else_condition_has_invalid_target()
    {
        var @if = new IfFlowStep
        {
            ElseStepId = Guid.NewGuid(),
        };

        var errors = await ValidateAsync(@if, new FlowDefinition());

        Assert.NotEmpty(errors);
    }

    [Fact]
    public async Task Should_not_add_error_if_ids_are_empty()
    {
        var @if = new IfFlowStep
        {
            Branches =
            [
                new IfFlowBranch { NextStepId = Guid.Empty },
            ],
            ElseStepId = Guid.Empty,
        };

        var errors = await ValidateAsync(@if, new FlowDefinition());

        Assert.Empty(errors);
    }

    [Fact]
    public async Task Should_not_add_error_if_else_condition_has_invalid_target_but_flow_definition_is_null()
    {
        var @if = new IfFlowStep
        {
            ElseStepId = Guid.NewGuid(),
        };

        var errors = await ValidateAsync(@if, null);

        Assert.Empty(errors);
    }

    [Fact]
    public async Task Should_not_add_error_if_ids_are_null()
    {
        var @if = new IfFlowStep
        {
            Branches =
            [
                new IfFlowBranch { NextStepId = null },
            ],
            ElseStepId = null,
        };

        var errors = await ValidateAsync(@if, new FlowDefinition());

        Assert.Empty(errors);
    }

    [Fact]
    public async Task Should_not_add_error_if_ids_are_valid()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var definition = new FlowDefinition
        {
            Steps = new Dictionary<Guid, FlowStepDefinition>
            {
                [id1] = new FlowStepDefinition { Step = new IfFlowStep() },
                [id2] = new FlowStepDefinition { Step = new IfFlowStep() },
            },
        };

        var @if = new IfFlowStep
        {
            Branches =
            [
                new IfFlowBranch { NextStepId = id1 },
            ],
            ElseStepId = id2,
        };

        var errors = await ValidateAsync(@if, definition);

        Assert.Empty(errors);
    }

    [Fact]
    public async Task Should_continue_with_first_matching_empty_branch()
    {
        var idBranch1 = Guid.NewGuid();
        var idBranch2 = Guid.NewGuid();
        var idBranch3 = Guid.NewGuid();
        var idElse = Guid.NewGuid();

        var @if = new IfFlowStep
        {
            Branches =
            [
                new IfFlowBranch { NextStepId = idBranch1 },
                new IfFlowBranch { NextStepId = idBranch2 },
                new IfFlowBranch { NextStepId = idBranch3 },
            ],
            ElseStepId = idElse,
        };

        var result = await @if.ExecuteAsync(executionContext, default);

        Assert.Equal(idBranch1, result.StepId);

        A.CallTo(() => expressionEngine.Evaluate(A<string>._, executionContext.Context))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Should_continue_with_first_matching_branch()
    {
        var idBranch1 = Guid.NewGuid();
        var idBranch2 = Guid.NewGuid();
        var idBranch3 = Guid.NewGuid();
        var idElse = Guid.NewGuid();

        A.CallTo(() => expressionEngine.Evaluate("A == 2", executionContext.Context))
            .Returns(true);

        var @if = new IfFlowStep
        {
            Branches =
            [
                new IfFlowBranch { NextStepId = idBranch1, Condition = "A == 1" },
                new IfFlowBranch { NextStepId = idBranch2, Condition = "A == 2" },
                new IfFlowBranch { NextStepId = idBranch3, Condition = "A == 3" },
            ],
            ElseStepId = idElse,
        };

        var result = await @if.ExecuteAsync(executionContext, default);

        Assert.Equal(idBranch2, result.StepId);

        A.CallTo(() => expressionEngine.Evaluate(A<string>._, executionContext.Context))
            .MustHaveHappenedANumberOfTimesMatching(x => x == 2);
    }

    [Fact]
    public async Task Should_continue_with_else_branch()
    {
        var idBranch1 = Guid.NewGuid();
        var idBranch2 = Guid.NewGuid();
        var idBranch3 = Guid.NewGuid();
        var idElse = Guid.NewGuid();

        var @if = new IfFlowStep
        {
            Branches =
            [
                new IfFlowBranch { NextStepId = idBranch1, Condition = "A == 1" },
                new IfFlowBranch { NextStepId = idBranch2, Condition = "A == 2" },
                new IfFlowBranch { NextStepId = idBranch3, Condition = "A == 3" },
            ],
            ElseStepId = idElse,
        };

        var result = await @if.ExecuteAsync(executionContext, default);

        Assert.Equal(idElse, result.StepId);

        A.CallTo(() => expressionEngine.Evaluate(A<string>._, executionContext.Context))
            .MustHaveHappenedANumberOfTimesMatching(x => x == 3);
    }

    [Fact]
    public async Task Should_continue_with_else_branch_for_empty_branches()
    {
        var idElse = Guid.NewGuid();

        var @if = new IfFlowStep
        {
            Branches = [],
            ElseStepId = idElse,
        };

        var result = await @if.ExecuteAsync(executionContext, default);

        Assert.Equal(idElse, result.StepId);

        A.CallTo(() => expressionEngine.Evaluate(A<string>._, executionContext.Context))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Should_continue_with_else_branch_for_null_branches()
    {
        var idElse = Guid.NewGuid();

        var @if = new IfFlowStep
        {
            Branches = null,
            ElseStepId = idElse,
        };

        var result = await @if.ExecuteAsync(executionContext, default);

        Assert.Equal(idElse, result.StepId);

        A.CallTo(() => expressionEngine.Evaluate(A<string>._, executionContext.Context))
            .MustNotHaveHappened();
    }

    [Fact]
    public void Should_provide_correct_equals0()
    {
        var lhs = new FlowDefinition();
        var rhs = new FlowDefinition();

        Assert.Equal(lhs, rhs);
        Assert.Equal(lhs.GetHashCode(), rhs.GetHashCode());
    }

    [Fact]
    public void Should_provide_correct_equals1()
    {
        var lhs = new IfFlowStep { Branches = null };
        var rhs = new IfFlowStep { Branches = null };

        Assert.Equal(lhs, rhs);
        Assert.Equal(lhs.GetHashCode(), rhs.GetHashCode());
    }

    [Fact]
    public void Should_provide_correct_equals2()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var lhs = new IfFlowStep
        {
            Branches =
            [
                new IfFlowBranch { Condition = "Condition1", NextStepId = id1 },
                new IfFlowBranch { Condition = "Condition2", NextStepId = id2 },
            ],
            ElseStepId = id1,
        };

        var rhs = new IfFlowStep
        {
            Branches =
            [
                new IfFlowBranch { Condition = "Condition1", NextStepId = id1 },
                new IfFlowBranch { Condition = "Condition2", NextStepId = id2 },
            ],
            ElseStepId = id1,
        };

        Assert.Equal(lhs, rhs);
        Assert.Equal(lhs.GetHashCode(), rhs.GetHashCode());
    }

    [Fact]
    public void Should_provide_correct_equals3()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var @base = new IfFlowStep
        {
            Branches =
            [
                new IfFlowBranch { Condition = "Condition1", NextStepId = id1 },
                new IfFlowBranch { Condition = "Condition2", NextStepId = id2 },
            ],
            ElseStepId = id1,
        };

        var otherElse = new IfFlowStep
        {
            Branches =
            [
                new IfFlowBranch { Condition = "Condition1", NextStepId = id1 },
                new IfFlowBranch { Condition = "Condition2", NextStepId = id2 },
            ],
            ElseStepId = id2,
        };

        Assert.NotEqual(@base, otherElse);
        Assert.NotEqual(@base.GetHashCode(), otherElse.GetHashCode());

        var otherBranchOrder = new IfFlowStep
        {
            Branches =
            [
                new IfFlowBranch { Condition = "Condition2", NextStepId = id2 },
                new IfFlowBranch { Condition = "Condition1", NextStepId = id1 },
            ],
            ElseStepId = id1,
        };

        Assert.NotEqual(@base, otherBranchOrder);
        Assert.NotEqual(@base.GetHashCode(), otherBranchOrder.GetHashCode());

        var nullBranches = new IfFlowStep
        {
            Branches = null,
            ElseStepId = id1,
        };

        Assert.NotEqual(@base, nullBranches);
        Assert.NotEqual(@base.GetHashCode(), nullBranches.GetHashCode());

        var emptyBranches = new IfFlowStep
        {
            Branches = [],
            ElseStepId = id1,
        };

        Assert.NotEqual(@base, emptyBranches);
        Assert.NotEqual(@base.GetHashCode(), emptyBranches.GetHashCode());
    }

    [Fact]
    public void Should_provide_correct_equals_with_null()
    {
        var lhs = new IfFlowStep();

        Assert.NotEqual(null!, lhs);
    }

    [Fact]
    public void Should_provide_correct_equals_with_same()
    {
        var lhs = new IfFlowStep();
        var rhs = lhs;

        Assert.Equal(lhs, rhs);
        Assert.Equal(lhs.GetHashCode(), rhs.GetHashCode());
    }

    private static async Task<List<(string Path, string Message)>> ValidateAsync(FlowStep step, FlowDefinition? definition)
    {
        var errors = new List<(string Path, string Message)>();

        var context = new FlowValidationContext(null!, definition);
        await step.ValidateAsync(context, (p, m) => errors.Add((p, m)), default);

        return errors;
    }
}
