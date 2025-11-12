// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json.Serialization;

namespace Squidex.Text.RichText.Model;

public class Mark : IMark
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("attrs")]
    public Attributes? Attributes { get; set; }

    public int GetIntAttr(string name, int defaultValue = 0)
    {
        return Attributes?.GetIntAttr(name, defaultValue) ?? defaultValue;
    }

    public string GetStringAttr(string name, string defaultValue = "")
    {
        return Attributes?.GetStringAttr(name, defaultValue) ?? defaultValue;
    }
}
