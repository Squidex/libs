// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Reflection;

namespace Squidex.Flows.Internal.Execution;

internal static class Extensions
{
    public static async ValueTask EvaluateExpressionsAsync(this FlowStep step, FlowExecutionContext executionContext)
    {
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
            var expressionResult = await executionContext.RenderAsync(expressionSource, executionContext.Context, attribute.Fallback);

            property.SetValue(step, expressionResult, null);
        }
    }
}
