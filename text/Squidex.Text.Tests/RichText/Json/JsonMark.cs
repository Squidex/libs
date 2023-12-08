// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Text.RichText.Model;

namespace Squidex.RichText.Json;

internal sealed class JsonMark : IMark
{
    private JsonObject? attrs;

    public MarkType Type { get; private set; }

    public bool TryUse(JsonObject source)
    {
        attrs = null;

        var isValid = true;
        foreach (var (key, value) in source)
        {
            switch (key)
            {
                case "type" when value.TryGetEnum<MarkType>(out var type):
                    Type = type;
                    break;
                case "attrs" when value is JsonObject attrs:
                    this.attrs = attrs;
                    break;
                default:
                    isValid = false;
                    break;
            }
        }

        return isValid;
    }

    public int GetIntAttr(string name, int defaultValue = 0)
    {
        return attrs.GetIntAttr(name, defaultValue);
    }

    public string GetStringAttr(string name, string defaultValue = "")
    {
        return attrs.GetStringAttr(name, defaultValue);
    }
}
