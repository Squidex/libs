// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;

namespace Squidex.Text.RichText.Model;

public sealed class Attributes : Dictionary<string, object>
{
    public int GetIntAttr(string name, int defaultValue = 0)
    {
        if (!TryGetValue(name, out var attr))
        {
            return defaultValue;
        }

        if (attr is int value)
        {
            return value;
        }

        if (attr is JsonElement element && element.ValueKind == JsonValueKind.Number)
        {
            return element.GetInt32()!;
        }

        return defaultValue;
    }

    public string GetStringAttr(string name, string defaultValue = "")
    {
        if (!TryGetValue(name, out var attr))
        {
            return defaultValue;
        }

        if (attr is string value)
        {
            return value;
        }

        if (attr is JsonElement element && element.ValueKind == JsonValueKind.String)
        {
            return element.GetString()!;
        }

        return defaultValue;
    }
}
