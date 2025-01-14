// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;
using Squidex.Events.Utils;

namespace Squidex.Events;

public sealed class EnvelopeHeaders : Dictionary<string, HeaderValue>
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    static EnvelopeHeaders()
    {
        Options.Converters.Add(new HeaderValueConverter());
    }

    public EnvelopeHeaders()
    {
    }

    public EnvelopeHeaders(IDictionary<string, HeaderValue> headers)
        : base(headers)
    {
    }

    public EnvelopeHeaders CloneHeaders()
    {
        return new EnvelopeHeaders(this);
    }

    public string SerializeToJsonString()
    {
        return JsonSerializer.Serialize(this, Options);
    }

    public byte[] SerializeToJsonBytes()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this, Options);
    }

    public static EnvelopeHeaders DeserializeFromJson(string json)
    {
        return JsonSerializer.Deserialize<EnvelopeHeaders>(json, Options) ??
            throw new JsonException("Failed to deserialize EventData.");
    }

    public static EnvelopeHeaders DeserializeFromJson(ReadOnlySpan<byte> json)
    {
        return JsonSerializer.Deserialize<EnvelopeHeaders>(json, Options) ??
            throw new JsonException("Failed to deserialize EventData.");
    }
}
