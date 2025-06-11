// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Flows.Internal;
using Squidex.Flows.Steps;

namespace Squidex.Flows;

public class FlowDefinitionTests
{
    [Fact]
    public void Should_provide_correct_equals_with_null()
    {
        var lhs = new FlowDefinition();

        Assert.NotEqual(null!, lhs);
    }

    [Fact]
    public void Should_provide_correct_equals_with_same()
    {
        var lhs = new FlowDefinition();
        var rhs = lhs;

        Assert.Equal(lhs, rhs);
        Assert.Equal(lhs.GetHashCode(), rhs.GetHashCode());
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
        var lhs = new FlowDefinition { Steps = null! };
        var rhs = new FlowDefinition { Steps = null! };

        Assert.Equal(lhs, rhs);
        Assert.Equal(lhs.GetHashCode(), rhs.GetHashCode());
    }

    [Fact]
    public void Should_provide_correct_equals2()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var @base = new FlowDefinition
        {
            Steps = new Dictionary<Guid, FlowStepDefinition>
            {
                [id1] = new FlowStepDefinition { Step = new DelayFlowStep() },
                [id2] = new FlowStepDefinition { Step = new IfFlowStep() },
            },
            InitialStepId = id1,
        };

        var sameStepOrder = new FlowDefinition
        {
            Steps = new Dictionary<Guid, FlowStepDefinition>
            {
                [id1] = new FlowStepDefinition { Step = new DelayFlowStep() },
                [id2] = new FlowStepDefinition { Step = new IfFlowStep() },
            },
            InitialStepId = id1,
        };

        Assert.Equal(@base, sameStepOrder);
        Assert.Equal(@base.GetHashCode(), sameStepOrder.GetHashCode());

        var swappedStepOrder = new FlowDefinition
        {
            Steps = new Dictionary<Guid, FlowStepDefinition>
            {
                [id2] = new FlowStepDefinition { Step = new IfFlowStep() },
                [id1] = new FlowStepDefinition { Step = new DelayFlowStep() },
            },
            InitialStepId = id1,
        };

        Assert.Equal(@base, swappedStepOrder);
        Assert.Equal(@base.GetHashCode(), swappedStepOrder.GetHashCode());
    }

    [Fact]
    public void Should_provide_correct_equals3()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var @base = new FlowDefinition
        {
            Steps = new Dictionary<Guid, FlowStepDefinition>
            {
                [id1] = new FlowStepDefinition { Step = new DelayFlowStep() },
                [id2] = new FlowStepDefinition { Step = new IfFlowStep() },
            },
            InitialStepId = id1,
        };

        var otherInitial = new FlowDefinition
        {
            Steps = new Dictionary<Guid, FlowStepDefinition>
            {
                [id1] = new FlowStepDefinition { Step = new DelayFlowStep() },
                [id2] = new FlowStepDefinition { Step = new IfFlowStep() },
            },
            InitialStepId = id2,
        };

        Assert.NotEqual(@base, otherInitial);
        Assert.NotEqual(@base.GetHashCode(), otherInitial.GetHashCode());

        var otherSteps = new FlowDefinition
        {
            Steps = new Dictionary<Guid, FlowStepDefinition>
            {
                [id1] = new FlowStepDefinition { Step = new IfFlowStep() },
                [id2] = new FlowStepDefinition { Step = new IfFlowStep() },
            },
            InitialStepId = id1,
        };

        Assert.NotEqual(@base, otherSteps);
        Assert.NotEqual(@base.GetHashCode(), otherSteps.GetHashCode());

        var nullSteps = new FlowDefinition
        {
            Steps = null!,
            InitialStepId = id1,
        };

        Assert.NotEqual(@base, nullSteps);
        Assert.NotEqual(@base.GetHashCode(), nullSteps.GetHashCode());

        var emptyStpes = new FlowDefinition
        {
            Steps = [],
            InitialStepId = id1,
        };

        Assert.NotEqual(@base, emptyStpes);
        Assert.NotEqual(@base.GetHashCode(), emptyStpes.GetHashCode());
    }
}
