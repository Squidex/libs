// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using HtmlPerformanceKit;
using Squidex.Text.Svg;

namespace Squidex.Text;

public static class SvgExtensions
{
    public static readonly HashSet<string> AllowedUriSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "http",
        "https",
    };

    public static bool IsValidSvg(this string html)
    {
        return GetSvgErrors(html).Count == 0;
    }

    public static SvgMetadata GetSvgMetadata(this string html)
    {
        var viewBox = string.Empty;
        var viewWidth = string.Empty;
        var viewHeight = string.Empty;

        var reader = new HtmlReader(new StringReader(html));

        while (reader.Read())
        {
            if (reader.TokenKind != HtmlTokenKind.Tag || !reader.NameAsMemory.Span.Equals("svg", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            for (var i = 0; i < reader.AttributeCount; i++)
            {
                var attributeValue = reader.GetAttributeAsMemory(i).Span.Trim();

                if (attributeValue.Length == 0)
                {
                    continue;
                }

                var attributeName = reader.GetAttributeNameAsMemory(i).Span;

                if (attributeName.Equals("width", StringComparison.OrdinalIgnoreCase))
                {
                    viewWidth = new string(attributeValue);
                }
                else if (attributeName.Equals("height", StringComparison.OrdinalIgnoreCase))
                {
                    viewHeight = new string(attributeValue);
                }
                else if (attributeName.Equals("viewbox", StringComparison.OrdinalIgnoreCase))
                {
                    viewBox = new string(attributeValue);
                }
            }

            break;
        }

        return new SvgMetadata(viewWidth, viewHeight, viewBox);
    }

    public static List<SvgError> GetSvgErrors(this string html)
    {
        var htmlErrors = new List<SvgError>();
        var htmlReader = new HtmlReader(new StringReader(html));

        AddErrors(htmlReader, htmlErrors);

        return htmlErrors;
    }

    private static void AddErrors(HtmlReader reader, List<SvgError> errors)
    {
        while (reader.Read())
        {
            if (reader.TokenKind != HtmlTokenKind.Tag)
            {
                continue;
            }

            var name = reader.NameAsMemory;

            if (!SvgElements.Allowed.Contains(name))
            {
                errors.Add(new SvgError($"Invalid element '{reader.Name}'",
                    reader.LineNumber,
                    reader.LinePosition));
                continue;
            }

            for (var i = 0; i < reader.AttributeCount; i++)
            {
                var attributeName = reader.GetAttributeNameAsMemory(i);

                if (attributeName.Span.StartsWith("data-", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!SvgAttributes.Allowed.Contains(attributeName))
                {
                    errors.Add(new SvgError($"Invalid attribute '{attributeName}'",
                        reader.LineNumber,
                        reader.LinePosition));
                }
                else if (SvgAttributes.Urls.Contains(attributeName))
                {
                    var attributeValue = reader.GetAttribute(i);

                    if (!Uri.TryCreate(attributeValue, UriKind.RelativeOrAbsolute, out var uri))
                    {
                        errors.Add(new SvgError($"Invalid URL for attribute '{attributeName}'",
                            reader.LineNumber,
                            reader.LinePosition));
                    }
                    else
                    {
                        if (uri.IsAbsoluteUri && !AllowedUriSchemes.Contains(uri.Scheme))
                        {
                            errors.Add(new SvgError($"Invalid URL scheme '{uri.Scheme}' for attribute '{attributeName}'",
                                reader.LineNumber,
                                reader.LinePosition));
                        }
                    }
                }
            }
        }
    }
}
