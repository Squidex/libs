// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Flows.Internal.Execution;
using Squidex.Flows.Steps;

namespace Squidex.Flows;

public class ScriptStepTests
{
    private readonly IFlowExpressionEngine expressionEngine = A.Fake<IFlowExpressionEngine>();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Should_do_nothing_if_no_script_defined(string? script)
    {
        var sut = new ScriptStep { Script = script! };

        await sut.ExecuteAsync(null!, default);

        A.CallTo(expressionEngine)
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Should_render_if_script_defined()
    {
        var sut = new ScriptStep { Script = "my-script" };

        var executionContext =
            new FlowExecutionContext(
                expressionEngine,
                A.Fake<FlowStep>(),
                A.Fake<IServiceProvider>(),
                new TestFlowContext(),
                null!,
                false);

        await sut.ExecuteAsync(executionContext, default);

        A.CallTo(() => expressionEngine.RenderAsync("my-script", executionContext.Context, ExpressionFallback.None))
            .MustHaveHappened();
    }
}
