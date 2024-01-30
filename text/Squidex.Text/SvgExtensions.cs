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
        "https"
    };

    public static bool IsValidSvg(this string html)
    {
        return GetSvgErrors(html).Count == 0;
    }

    public static SvgMetadata GetSvgMetadata(this string html)
    {
        string width = string.Empty, height = string.Empty, viewBox = string.Empty;

        using (var reader = new HtmlReader(new StringReader(html)))
        {
            while (reader.Read())
            {
                if (reader.TokenKind == HtmlTokenKind.Tag && reader.Name == "svg")
                {
                    for (var i = 0; i < reader.AttributeCount; i++)
                    {
                        var attributeName = reader.GetAttributeName(i);
                        var attributeValue = reader.GetAttribute(i);

                        if (string.IsNullOrWhiteSpace(attributeValue))
                        {
                            continue;
                        }

                        if (string.Equals(attributeName, "width", StringComparison.OrdinalIgnoreCase))
                        {
                            width = attributeValue.Trim();
                        }
                        else if (string.Equals(attributeName, "height", StringComparison.OrdinalIgnoreCase))
                        {
                            height = attributeValue.Trim();
                        }
                        else if (string.Equals(attributeName, "viewBox", StringComparison.OrdinalIgnoreCase))
                        {
                            viewBox = attributeValue.Trim();
                        }
                    }
                }
            }
        }

        return new SvgMetadata(width, height, viewBox);
    }

    public static List<SvgError> GetSvgErrors(this string html)
    {
        var errors = new List<SvgError>();

        using (var reader = new HtmlReader(new StringReader(html)))
        {
            AddErrors(reader, errors);
        }

        return errors;
    }

    private static void AddErrors(HtmlReader reader, List<SvgError> errors)
    {
        while (reader.Read())
        {
            if (reader.TokenKind != HtmlTokenKind.Tag)
            {
                continue;
            }

            if (!SvgElements.Allowed.Contains(reader.Name))
            {
                errors.Add(new SvgError($"Invalid element '{reader.Name}'",
                    reader.LineNumber,
                    reader.LinePosition));
            }

            for (var i = 0; i < reader.AttributeCount; i++)
            {
                var attributeName = reader.GetAttributeName(i);
                var attributeValue = reader.GetAttribute(i);

                if (!SvgAttributes.Allowed.Contains(attributeName))
                {
                    errors.Add(new SvgError($"Invalid attribute '{attributeName}'",
                        reader.LineNumber,
                        reader.LinePosition));
                }
                else if (SvgAttributes.Urls.Contains(attributeName))
                {
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
