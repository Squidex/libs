// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Web.UI;

namespace Squidex.Text.RichText;

internal static class HtmlExtensions
{
    public static void AddNonEmptyAttribute(this HtmlTextWriter writer, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        writer.AddAttribute(name, value, encode: true);
    }

    public static void AddNonEmptyAttribute(this HtmlTextWriter writer, string name, string value, Func<string, string> formatter)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        writer.AddAttribute(name, formatter(value), encode: true);
    }
}
