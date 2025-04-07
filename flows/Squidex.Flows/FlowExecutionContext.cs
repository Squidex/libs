// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using Squidex.Flows.Internal.Execution;

namespace Squidex.Flows;

public sealed class FlowExecutionContext(
    IFlowExpressionEngine expressionEngine,
    FlowStep step,
    IServiceProvider serviceProvider,
    FlowContext context,
    Action<string, string?> logger,
    bool isSimulation)
{
    public bool IsSimulation => isSimulation;

    public FlowContext Context => context;

    public FlowStep Step => step;

    public T Resolve<T>() where T : class
    {
        return serviceProvider.GetRequiredService<T>();
    }

    public void Log(string message, object? dump = null)
    {
        string? formattedDump = null;

        if (dump is string text)
        {
            formattedDump = text;
        }
        else if (dump is Exception exception)
        {
            formattedDump = exception.Message;
        }
        else if (dump != null)
        {
            formattedDump = Serialize(dump);
        }

        logger(message, formattedDump);
    }

    public bool Evaluate<T>(string? expression, T value)
    {
        return expressionEngine.Evaluate(expression, value);
    }

    public ValueTask<string?> RenderAsync<T>(string? expression, T value, ExpressionFallback fallback = default)
    {
        return expressionEngine.RenderAsync(expression, value, fallback);
    }

    public string Serialize<T>(T value)
    {
        return expressionEngine.Serialize(value);
    }
}
