// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

using System.Text.Json;
using Squidex.Events.Utils;

namespace Squidex.Events;

public sealed record EventData(string Type, EnvelopeHeaders Headers, string Payload)
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    static EventData()
    {
        Options.Converters.Add(new HeaderValueConverter());
    }

    public string SerializeToJsonString()
    {
        return JsonSerializer.Serialize(this, Options);
    }

    public static EventData DeserializeFromJson(string json)
    {
        return JsonSerializer.Deserialize<EventData>(json, Options) ??
            throw new JsonException("Failed to deserialize EventData.");
    }
}
