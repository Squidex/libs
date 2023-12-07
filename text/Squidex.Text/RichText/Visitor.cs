// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Text.RichText.Model;

namespace Squidex.Text.RichText;

public abstract class Visitor<T>
{
    public T Visit(Node node)
    {
        Func<T> inner = () =>
        {
            switch (node.Type)
            {
                case NodeType.Blockquote:
                    return VisitBlockquote(node);
                case NodeType.BulletList:
                    return VisitBulletList(node);
                case NodeType.CodeBlock:
                    return VisitCodeBlock(node);
                case NodeType.Document:
                    return VisitDocument(node);
                case NodeType.HardBreak:
                    return VisitHardBreak(node);
                case NodeType.Heading:
                    return VisitHeading(node);
                case NodeType.Image:
                    return VisitImage(node);
                case NodeType.HorizontalLine:
                    return VisitHorizontalLine(node);
                case NodeType.ListItem:
                    return VisitListItem(node);
                case NodeType.OrderedList:
                    return VisitOrderedList(node);
                case NodeType.Paragraph:
                    return VisitParagraph(node);
                case NodeType.Text:
                    return VisitText(node);
                default:
                    ThrowInvalidType(node.Type);
                    return default!;
            }
        };

        if (node.Marks != null)
        {
            foreach (var mark in node.Marks.Reverse())
            {
                var currentInner = inner;

                switch (mark.Type)
                {
                    case MarkType.Bold:
                        inner = () => VisitBold(mark, currentInner);
                        break;
                    case MarkType.Code:
                        inner = () => VisitCode(mark, currentInner);
                        break;
                    case MarkType.Italic:
                        inner = () => VisitItalic(mark, currentInner);
                        break;
                    case MarkType.Link:
                        inner = () => VisitLink(mark, currentInner);
                        break;
                    case MarkType.Underline:
                        inner = () => VisitUnderline(mark, currentInner);
                        break;
                    default:
                        ThrowInvalidType(mark.Type);
                        return default!;
                }
            }
        }

        return inner();
    }

    protected virtual T VisitBold(Mark mark, Func<T> inner)
    {
        return inner();
    }

    protected virtual T VisitCode(Mark mark, Func<T> inner)
    {
        return inner();
    }

    protected virtual T VisitItalic(Mark mark, Func<T> inner)
    {
        return inner();
    }

    protected virtual T VisitLink(Mark mark, Func<T> inner)
    {
        return inner();
    }

    protected virtual T VisitUnderline(Mark mark, Func<T> inner)
    {
        return inner();
    }

    protected virtual T VisitBlockquote(Node node)
    {
        VisitChildren(node);
        return default!;
    }

    protected virtual T VisitBulletList(Node node)
    {
        VisitChildren(node);
        return default!;
    }

    protected virtual T VisitCodeBlock(Node node)
    {
        VisitChildren(node);
        return default!;
    }

    protected virtual T VisitDocument(Node node)
    {
        VisitChildren(node);
        return default!;
    }

    protected virtual T VisitHardBreak(Node node)
    {
        VisitChildren(node);
        return default!;
    }

    protected virtual T VisitHeading(Node node)
    {
        VisitChildren(node);
        return default!;
    }

    protected virtual T VisitHorizontalLine(Node node)
    {
        VisitChildren(node);
        return default!;
    }

    protected virtual T VisitImage(Node node)
    {
        VisitChildren(node);
        return default!;
    }

    protected virtual T VisitListItem(Node node)
    {
        VisitChildren(node);
        return default!;
    }

    protected virtual T VisitOrderedList(Node node)
    {
        VisitChildren(node);
        return default!;
    }

    protected virtual T VisitParagraph(Node node)
    {
        VisitChildren(node);
        return default!;
    }

    protected virtual T VisitText(Node node)
    {
        VisitChildren(node);
        return default!;
    }

    protected void VisitChildren(Node node)
    {
        if (node.Content == null)
        {
            return;
        }

        foreach (var content in node.Content)
        {
            Visit(content);
        }
    }

    private static void ThrowInvalidType(MarkType type)
    {
        throw new InvalidOperationException($"Invalid type '{type}'.");
    }

    private static void ThrowInvalidType(NodeType type)
    {
        throw new InvalidOperationException($"Invalid type '{type}'.");
    }
}
