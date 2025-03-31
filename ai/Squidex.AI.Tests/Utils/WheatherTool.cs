// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.Utils;

public sealed class WheatherTool : IChatTool
{
    public ToolSpec Spec { get; } =
        new ToolSpec("wheather", "Wheather", "Gets the temperatore at a location.")
        {
            Arguments =
            {
                ["location"] = new ToolStringArgumentSpec("The location")
                {
                    IsRequired = true,
                },
            },
        };

    public async Task<string> ExecuteAsync(ToolContext toolContext,
        CancellationToken ct)
    {
        var location = toolContext.Arguments["location"].AsString;

        await Task.Yield();

        if (location == "Berlin")
        {
            return "{ \"temperature\": 22.42 }";
        }

        return "{ \"temperature\": -44.13 }";
    }
}
