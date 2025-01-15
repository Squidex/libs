// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;

namespace Squidex.Events;

public static class EnvelopeExtensions
{
    public static long GetLong(this EnvelopeHeaders obj, string key)
    {
        if (obj.TryGetValue(key, out var found))
        {
            if (found is HeaderNumberValue n)
            {
                return n.Value;
            }
            else if (found is HeaderStringValue s && double.TryParse(s.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return (long)result;
            }
        }

        return 0;
    }

    public static string GetString(this EnvelopeHeaders obj, string key)
    {
        if (obj.TryGetValue(key, out var found))
        {
            return found.ToString();
        }

        return string.Empty;
    }

    public static bool GetBoolean(this EnvelopeHeaders obj, string key)
    {
        if (obj.TryGetValue(key, out var found) && found is HeaderBooleanValue b)
        {
            return b.Value;
        }

        return false;
    }
}
