// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using HtmlAgilityPack;

namespace Squidex.Text;

public static class HtmlSvgExtensions
{
    private static readonly HashSet<string> InvalidSvgElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "script",
        "iframe"
    };

    public static bool IsValidSvg(this string html)
    {
        var document = LoadHtml(html);

        return IsValid(document.DocumentNode);
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

    private static bool IsValid(HtmlNode node)
    {
        switch (node.NodeType)
        {
            case HtmlNodeType.Document:
                return IsChildrenValid(node);

            case HtmlNodeType.Element:
                if (InvalidSvgElements.Contains(node.Name))
                {
                    return false;
                }

                if (node.HasAttributes)
                {
                    for (var i = 0; i < node.Attributes.Count; i++)
                    {
                        var attribute = node.Attributes[i];

                        if (attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }
                }

                if (node.HasChildNodes)
                {
                    return IsChildrenValid(node);
                }

                break;
        }

        return true;
    }

    private static bool IsChildrenValid(HtmlNode node)
    {
        foreach (var child in node.ChildNodes)
        {
            if (!IsValid(child))
            {
                return false;
            }
        }

        return true;
    }

    private static void AddErrors(HtmlNode node, List<HtmlSvgError> errors)
    {
        switch (node.NodeType)
        {
            case HtmlNodeType.Document:
                AddChildrenErrors(node, errors);
                break;

            case HtmlNodeType.Element:
                if (InvalidSvgElements.Contains(node.Name))
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

                        if (attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                        {
                            errors.Add(new HtmlSvgError($"Invalid attribute '{attribute.Name}'",
                                attribute.Line,
                                attribute.LinePosition));
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
