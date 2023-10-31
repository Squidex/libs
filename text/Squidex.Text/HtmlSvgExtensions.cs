// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using HtmlAgilityPack;
using Squidex.Text.Svg;

namespace Squidex.Text;

public static class HtmlSvgExtensions
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

    public static List<HtmlSvgError> GetSvgErrors(this string html)
    {
        var errors = new List<HtmlSvgError>();

        var document = LoadHtml(html);

        AddErrors(document.DocumentNode, errors);

        return errors;
    }

    private static HtmlDocument LoadHtml(string text)
    {
        var document = new HtmlDocument();

        document.LoadHtml(text);

        return document;
    }

    private static void AddErrors(HtmlNode node, List<HtmlSvgError> errors)
    {
        switch (node.NodeType)
        {
            case HtmlNodeType.Document:
                AddChildrenErrors(node, errors);
                break;

            case HtmlNodeType.Element:
                if (!SvgElements.Allowed.Contains(node.Name))
                {
                    errors.Add(new HtmlSvgError($"Invalid element '{node.Name}'",
                        node.Line,
                        node.LinePosition));
                }

                if (node.HasAttributes)
                {
                    for (var i = 0; i < node.Attributes.Count; i++)
                    {
                        var attribute = node.Attributes[i];

                        if (!SvgAttributes.Allowed.Contains(attribute.Name))
                        {
                            errors.Add(new HtmlSvgError($"Invalid attribute '{attribute.Name}'",
                                attribute.Line,
                                attribute.LinePosition));
                        }
                        else if (SvgAttributes.Urls.Contains(attribute.Name))
                        {
                            if (!Uri.TryCreate(attribute.Value, UriKind.RelativeOrAbsolute, out var uri))
                            {
                                errors.Add(new HtmlSvgError($"Invalid URL for attribute '{attribute.Name}'",
                                    attribute.Line,
                                    attribute.LinePosition));
                            }
                            else
                            {
                                if (uri.IsAbsoluteUri && !AllowedUriSchemes.Contains(uri.Scheme))
                                {
                                    errors.Add(new HtmlSvgError($"Invalid URL scheme '{uri.Scheme}' for attribute '{attribute.Name}'",
                                        attribute.Line,
                                        attribute.LinePosition));
                                }
                            }
                        }
                    }
                }

                if (node.HasChildNodes)
                {
                    AddChildrenErrors(node, errors);
                }

                break;
        }
    }

    private static void AddChildrenErrors(HtmlNode node, List<HtmlSvgError> errors)
    {
        foreach (var child in node.ChildNodes)
        {
            AddErrors(child, errors);
        }
    }
}
