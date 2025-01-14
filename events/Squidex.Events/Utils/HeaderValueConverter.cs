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
    public override HeaderValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return new HeaderStringValue(reader.GetString()!);
            case JsonTokenType.Number:
                return new HeaderNumberValue(reader.GetInt64());
            case JsonTokenType.True:
                return new HeaderBooleanValue(true);
            case JsonTokenType.False:
                return new HeaderBooleanValue(false);
            default:
                throw new JsonException($"Unsupported token '{reader.TokenType}'.");
        }
    }

    public override void Write(Utf8JsonWriter writer, HeaderValue value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case HeaderStringValue s:
                writer.WriteStringValue(s.Value);
                break;
            case HeaderNumberValue n:
                writer.WriteNumberValue(n.Value);
                break;
            case HeaderBooleanValue b:
                writer.WriteBooleanValue(b.Value);
                break;
            default:
                throw new JsonException($"Unsupported value type '{value.GetType()}'.");
        }
    }
}
