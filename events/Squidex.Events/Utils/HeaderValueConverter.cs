// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Squidex.Events.Utils;

public sealed class HeaderValueConverter : JsonConverter<HeaderValue>
{
    public override HeaderValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString()!;
            case JsonTokenType.Number:
                return reader.GetDouble();
            case JsonTokenType.Null:
                return default;
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            default:
                throw new JsonException($"Unsupported token '{reader.TokenType}'.");
        }
    }

    public override void Write(Utf8JsonWriter writer, HeaderValue value, JsonSerializerOptions options)
    {
        switch (value.Value)
        {
            case string s:
                writer.WriteStringValue(s);
                break;
            case double n:
                writer.WriteNumberValue(n);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case null:
                writer.WriteNullValue();
                break;
            default:
                throw new JsonException($"Unsupported value type '{value.Value.GetType()}'.");
        }
    }
}
