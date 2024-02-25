// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAI.Builders;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.SharedModels;

namespace Squidex.Text.ChatBots.OpenAI;

internal static class Helper
{
    public static ToolDefinition ToToolDefinition(this ToolSpec spec)
    {
        var builder = new FunctionDefinitionBuilder(spec.Name, spec.Description);

        foreach (var argument in spec.Arguments)
        {
            switch (argument)
            {
                case ToolStringArgumentSpec:
                    builder.AddParameter(argument.Name,
                        PropertyDefinition.DefineString(argument.Description),
                        argument.IsRequired);
                    break;
                case ToolNumberArgumentSpec:
                    builder.AddParameter(argument.Name,
                        PropertyDefinition.DefineNumber(argument.Description),
                        argument.IsRequired);
                    break;
                case ToolEnumArgumentSpec enumArg:
                    builder.AddParameter(argument.Name,
                        PropertyDefinition.DefineEnum(enumArg.Values.ToList(), argument.Description),
                        argument.IsRequired);
                    break;
            }
        }

        return ToolDefinition.DefineFunction(builder.Build());
    }

    public static bool AppendMessage(this ChatCompletionCreateRequest request, ChatMessage message, int maxContextLength, int charactersPerToken)
    {
        if (string.IsNullOrWhiteSpace(message.Content))
        {
            return true;
        }

        var newTokens = CalculateTokens(message);
        if (newTokens > maxContextLength)
        {
            return false;
        }

        request.Messages.Add(message);

        var totalTokens = request.Messages.Sum(CalculateTokens);

        while (totalTokens > maxContextLength && request.Messages.Count > 0)
        {
            var first = request.Messages.RemoveAtAndReturn(0);

            totalTokens -= CalculateTokens(first);
        }

        return true;

        int CalculateTokens(ChatMessage message)
        {
            if (message.Content is not { Length: > 0 })
            {
                return 0;
            }

            return (int)Math.Floor((float)message.Content.Length / charactersPerToken);
        }
    }

    public static T RemoveAtAndReturn<T>(this IList<T> source, int index)
    {
        var item = source[index];

        source.RemoveAt(index);
        return item;
    }

    public static Dictionary<string, ToolValue> ParseArguments(this FunctionCall call, ToolSpec spec)
    {
        var result = new Dictionary<string, ToolValue>();

        if (string.IsNullOrWhiteSpace(call.Arguments))
        {
            return result;
        }

        if (JsonNode.Parse(call.Arguments) is not JsonObject values)
        {
            throw new ChatException("Argument is not an object.");
        }

        foreach (var argument in spec.Arguments)
        {
            values.TryGetPropertyValue(argument.Name, out var value);

            if (value == null || value.GetValueKind() == JsonValueKind.Null)
            {
                if (argument.IsRequired)
                {
                    throw new ChatException($"Parameter '{argument.Name}' is not part of the arguments, but required.");
                }

                result[argument.Name] = ToolValue.Null;
                continue;
            }

            var kind = value.GetValueKind();

            ToolValue parsed;
            switch (argument)
            {
                case ToolBooleanArgumentSpec _ when kind == JsonValueKind.True:
                    parsed = new ToolBooleanValue(true);
                    break;
                case ToolBooleanArgumentSpec _ when kind == JsonValueKind.False:
                    parsed = new ToolBooleanValue(false);
                    break;
                case ToolNumberArgumentSpec _ when kind == JsonValueKind.Number:
                    parsed = new ToolNumberValue((double)value.AsValue()!);
                    break;
                case ToolStringArgumentSpec _ when kind == JsonValueKind.String:
                    parsed = new ToolStringValue((string)value.AsValue()!);
                    break;
                case ToolEnumArgumentSpec stringArg when kind == JsonValueKind.String:
                    var stringValue = (string)value.AsValue()!;

                    if (!stringArg.Values.Contains(stringValue))
                    {
                        throw new ChatException($"Unexpected value '{stringValue}' for argument '{argument.Name}'.");
                    }

                    parsed = new ToolStringValue(stringValue);
                    break;
                default:
                    throw new ChatException($"Unexpected kind '{kind}' for argument '{argument.Name}'. Expected string or number.");
            }

            result[argument.Name] = parsed;
        }

        return result;
    }
}
