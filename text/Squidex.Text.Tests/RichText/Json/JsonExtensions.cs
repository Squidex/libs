// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.RichText.Json;

internal static class JsonExtensions
{
    public static int GetIntAttr(this JsonObject? attrs, string name, int defaultValue = 0)
    {
        if (attrs?.TryGetValue(name, out var value) == true && value is int attr)
        {
            return attr;
        }

        return defaultValue;
    }

    public static string GetStringAttr(this JsonObject? attrs, string name, string defaultValue = "")
    {
        if (attrs?.TryGetValue(name, out var value) == true && value is string attr)
        {
            return attr;
        }

        return defaultValue;
    }

    public static bool TryGetArrayOfObject(this object value, out JsonArray array)
    {
        array = default!;

        if (value is not JsonArray temp)
        {
            return false;
        }

        foreach (var item in temp)
        {
            if (item is not JsonObject)
            {
                return false;
            }
        }

        array = temp;
        return true;
    }
}
