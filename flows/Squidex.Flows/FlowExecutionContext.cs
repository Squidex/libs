// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Flows.Execution;

namespace Squidex.Flows;

public sealed class FlowExecutionContext(
    IExpressionEngine expressionEngine,
    IServiceProvider serviceProvider,
    Action<string> logger,
    bool isSimulation)
{
    public IServiceProvider ServiceProvider => serviceProvider;

    public bool IsSimulation => isSimulation;

    public void Log(string message)
    {
        logger(message);
    }

    public bool Evaluate<TContext>(string expression, TContext context)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        return expressionEngine.Evaluate(expression, context);
    }

    public string Execute<TContext>(string expression, TContext context)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return string.Empty;
        }

        return expressionEngine.Execute(expression, context);
    }
}
