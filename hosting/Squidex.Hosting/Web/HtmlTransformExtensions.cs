// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Squidex.Hosting.Web;

public static partial class HtmlTransformExtensions
{
    private static readonly Regex BaseRegex = BaseRegexFactory();

    [GeneratedRegex("<base[\\s]+href=\"\\/\"[\\s]*[/]{0,1}[\\s]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex BaseRegexFactory();

    public static string AdjustBase(this string html, HttpContext httpContext)
    {
        if (httpContext.Request.PathBase != null)
        {
            var @base = $"<base href=\"{httpContext.Request.PathBase}/\">";

            var newHtml = BaseRegex.Replace(html, @base);

            if (Equals(newHtml, html))
            {
                newHtml = html.Replace("<head>", "<head>" + @base, StringComparison.OrdinalIgnoreCase);
            }

            html = newHtml;
        }

        return html;
    }
}
