// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.RichText.Model;

internal static class JsonExtensions
{
    public static bool TryGetEnum<T>(this JsonValue value, out T enumValue) where T : struct
    {
        enumValue = default;

        return value.Value is string text && Enum.TryParse(text, true, out enumValue);
    }

    public static bool TryGetArrayOfObject(this JsonValue value, out JsonArray array)
    {
        array = default!;

        if (value.Value is not JsonArray temp)
        {
            return false;
        }

        foreach (var item in temp)
        {
            if (item.Type != JsonValueType.Object)
            {
                return false;
            }
        }

        array = temp;
        return false;
    }
}
