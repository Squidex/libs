// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.RichText.Model;

public static class NodeExtensions
{
    public static long GetNumber(this Attributed attributed, string key, long defaultValue)
    {
        if (attributed.Attributes == null)
        {
            return defaultValue;
        }

        if (attributed.Attributes.TryGetValue(key, out var value) && value.Kind == AttributeKind.Number)
        {
            return (long)value.AsNumber;
        }

        return defaultValue;
    }

    public static string GetString(this Attributed attributed, string key, string defaultValue = "")
    {
        if (attributed.Attributes == null)
        {
            return defaultValue;
        }

        if (attributed.Attributes.TryGetValue(key, out var value) && value.Kind == AttributeKind.String)
        {
            return value.AsString;
        }

        return defaultValue;
    }
}
