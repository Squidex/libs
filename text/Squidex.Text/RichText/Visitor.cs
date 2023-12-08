// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Text.RichText.Model;

namespace Squidex.Text.RichText;

public abstract class Visitor
{
    public void Visit(NodeBase node)
    {
        node.Reset();

        Action inner = () =>
        {
            var type = node.GetNodeType();

            switch (type)
            {
                case NodeType.Image:
                    VisitImage(node,
                        node.GetStringAttr("src"),
                        node.GetStringAttr("alt"),
                        node.GetStringAttr("title"));
                    break;
                case NodeType.CodeBlock:
                    VisitCodeBlock(node,
                        node.GetStringAttr("language"));
                    break;
                case NodeType.Heading:
                    VisitHeading(node,
                        node.GetIntAttr("level", 1));
                    break;
                case NodeType.Blockquote:
                    VisitBlockquote(node);
                    break;
                case NodeType.BulletList:
                    VisitBulletList(node);
                    break;
                case NodeType.Document:
                    VisitDocument(node);
                    break;
                case NodeType.HardBreak:
                    VisitHardBreak(node);
                    break;
                case NodeType.HorizontalLine:
                    VisitHorizontalLine(node);
                    break;
                case NodeType.ListItem:
                    VisitListItem(node);
                    break;
                case NodeType.OrderedList:
                    VisitOrderedList(node);
                    break;
                case NodeType.Paragraph:
                    VisitParagraph(node);
                    break;
                case NodeType.Text:
                    VisitText(node);
                    break;
                default:
                    ThrowInvalidType(type);
                    break;
            }
        };

        MarkBase? mark;
        while ((mark = node.GetNextMarkReverse()) != null)
        {
            var currentInner = inner;
            var currentMark = mark;

            var type = mark.GetMarkType();

            switch (type)
            {
                case MarkType.Link:
                    inner = () => VisitLink(currentMark, currentInner,
                        currentMark.GetStringAttr("href"),
                        currentMark.GetStringAttr("target"));
                    break;
                case MarkType.Bold:
                    inner = () => VisitBold(currentMark, currentInner);
                    break;
                case MarkType.Code:
                    inner = () => VisitCode(currentMark, currentInner);
                    break;
                case MarkType.Italic:
                    inner = () => VisitItalic(currentMark, currentInner);
                    break;
                case MarkType.Underline:
                    inner = () => VisitUnderline(currentMark, currentInner);
                    break;
                default:
                    ThrowInvalidType(type);
                    break;
            }
        }

        inner();
    }

    protected virtual void VisitBold(MarkBase mark, Action inner)
    {
        inner();
    }

    protected virtual void VisitCode(MarkBase mark, Action inner)
    {
        inner();
    }

    protected virtual void VisitItalic(MarkBase mark, Action inner)
    {
        inner();
    }

    protected virtual void VisitLink(MarkBase mark, Action inner, string? href, string? target)
    {
        inner();
    }

    protected virtual void VisitUnderline(MarkBase mark, Action inner)
    {
        inner();
    }

    protected virtual void VisitBlockquote(NodeBase node)
    {
        VisitChildren(node);
    }

    protected virtual void VisitBulletList(NodeBase node)
    {
        VisitChildren(node);
    }

    protected virtual void VisitCodeBlock(NodeBase node, string? language)
    {
        VisitChildren(node);
    }

    protected virtual void VisitDocument(NodeBase node)
    {
        VisitChildren(node);
    }

    protected virtual void VisitHardBreak(NodeBase node)
    {
        VisitChildren(node);
    }

    protected virtual void VisitHeading(NodeBase node, int level)
    {
        VisitChildren(node);
    }

    protected virtual void VisitHorizontalLine(NodeBase node)
    {
        VisitChildren(node);
    }

    protected virtual void VisitImage(NodeBase node, string? src, string? alt, string? title)
    {
        VisitChildren(node);
    }

    protected virtual void VisitListItem(NodeBase node)
    {
        VisitChildren(node);
    }

    protected virtual void VisitOrderedList(NodeBase node)
    {
        VisitChildren(node);
    }

    protected virtual void VisitParagraph(NodeBase node)
    {
        VisitChildren(node);
    }

    protected virtual void VisitText(NodeBase node)
    {
        VisitChildren(node);
    }

    protected void VisitChildren(NodeBase node)
    {
        NodeBase? child;
        while ((child = node.GetNextNode()) != null)
        {
            Visit(child);
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
