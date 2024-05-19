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
using OpenAIMessage = OpenAI.ObjectModels.RequestModels.ChatMessage;

namespace Squidex.AI.Implementation.OpenAI;

internal static class Helper
{
    public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            list.Add(item);
        }
    }

    public static ToolDefinition ToOpenAITool(this ToolSpec spec, string toolName)
    {
        var builder = new FunctionDefinitionBuilder(toolName, spec.Description);

        foreach (var (name, argument) in spec.Arguments)
        {
            PropertyDefinition property;
            switch (argument)
            {
                case ToolStringArgumentSpec:
                    property = PropertyDefinition.DefineString(argument.Description);
                    break;
                case ToolNumberArgumentSpec:
                    property = PropertyDefinition.DefineNumber(argument.Description);
                    break;
                case ToolEnumArgumentSpec enumArg:
                    property = PropertyDefinition.DefineEnum(enumArg.Values.ToList(), argument.Description);
                    break;
                default:
                    throw new InvalidOperationException("Invalid property tpye.");
            }

            builder.AddParameter(name, property);
        }

        return ToolDefinition.DefineFunction(builder.Build());
    }

    public static OpenAIMessage ToOpenAIMessage(this ChatMessage message)
    {
        switch (message.Type)
        {
            case ChatMessageType.System:
                return OpenAIMessage.FromSystem(message.Content);
            case ChatMessageType.Assistant:
                return OpenAIMessage.FromAssistant(message.Content);
            default:
                return OpenAIMessage.FromUser(message.Content);
        }
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

        foreach (var (name, argument) in spec.Arguments)
        {
            values.TryGetPropertyValue(name, out var value);

            if (value == null || value.GetValueKind() == JsonValueKind.Null)
            {
                if (argument.IsRequired)
                {
                    throw new ChatException($"Parameter '{name}' is not part of the arguments, but required.");
                }

                result[name] = ToolValue.Null;
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
                        throw new ChatException($"Unexpected value '{stringValue}' for argument '{name}'.");
                    }

                    parsed = new ToolStringValue(stringValue);
                    break;
                default:
                    throw new ChatException($"Unexpected kind '{kind}' for argument '{name}'. Expected string or number.");
            }

            result[name] = parsed;
        }

        return result;
    }
}
