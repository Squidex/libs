// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Flows.Internal.Execution;

namespace Squidex.Flows;

public class NoRetryErrorPolicyTests
{
    private readonly NoRetryErrorPolicy<TestFlowContext> sut = new NoRetryErrorPolicy<TestFlowContext>();

    [Fact]
    public void Should_not_retry_if_disabled_for_step()
    {
        var stepState = new FlowExecutionStepState
        {
            Attempts =
            [
                new FlowExecutionStepAttempt(),
            ],
        };

        var next = sut.ShouldRetry(null!, stepState, null!, default);

        Assert.Null(next);
    }
}
