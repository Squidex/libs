// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Text.RichText.Model;

namespace Squidex.RichText.Json;

internal sealed class JsonMark : MarkBase
{
    private JsonObject? attrs;
    private MarkType type;

    public bool TryUse(JsonObject source)
    {
        attrs = null;

        var isValid = true;
        foreach (var (key, value) in source)
        {
            switch (key)
            {
                case "type" when value.TryGetEnum<MarkType>(out var type):
                    this.type = type;
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

    public override MarkType GetMarkType()
    {
        return type;
    }

    public override int GetIntAttr(string name, int defaultValue = 0)
    {
        if (attrs?.TryGetValue(name, out var value) == true && value is int attr)
        {
            return attr;
        }

        return defaultValue;
    }

    public override string GetStringAttr(string name, string defaultValue = "")
    {
        if (attrs?.TryGetValue(name, out var value) == true && value is string attr)
        {
            return attr;
        }

        return defaultValue;
    }
}
