// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.Utils;

public sealed class MathTool : IChatTool
{
    public ToolSpec Spec { get; } =
        new ToolSpec("multiply", "Multiply", "Multiplies two numbers.")
        {
            Arguments =
            {
                ["lhs"] = new ToolNumberArgumentSpec("The left side hand number")
                {
                    IsRequired = true,
                },
                ["rhs"] = new ToolNumberArgumentSpec("The right side hand number")
                {
                    IsRequired = true,
                },
            }
        };

    public async Task<string> ExecuteAsync(IChatAgent agent, ChatContext context, Dictionary<string, ToolValue> arguments,
        CancellationToken ct)
    {
        var lhs = arguments["lhs"].AsNumber;
        var rhs = arguments["rhs"].AsNumber;

        await Task.Yield();
        return $"The result {(lhs * rhs) + 42}. Return this value to the user.";
    }
}
