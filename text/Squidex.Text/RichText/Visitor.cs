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
    private readonly Action visitInner;
    private NodeBase currentNode;

    protected Visitor()
    {
        visitInner = VisitCurrentMarkOrNode;
    }

    public void Visit(NodeBase node)
    {
        currentNode = node;
        currentNode.Reset();

        VisitCurrentMarkOrNode();
    }

    private void VisitCurrentMarkOrNode()
    {
        var mark = currentNode.GetNextMark();

        if (mark != null)
        {
            VisitMark(mark);
        }
        else
        {
            VisitNode(currentNode);
        }
    }

    private void VisitMark(MarkBase mark)
    {
        var type = mark.GetMarkType();

        switch (type)
        {
            case MarkType.Link:
                VisitLink(mark, visitInner,
                    mark.GetStringAttr("href"),
                    mark.GetStringAttr("target"));
                break;
            case MarkType.Bold:
                VisitBold(mark, visitInner);
                break;
            case MarkType.Code:
                VisitCode(mark, visitInner);
                break;
            case MarkType.Italic:
                VisitItalic(mark, visitInner);
                break;
            case MarkType.Underline:
                VisitUnderline(mark, visitInner);
                break;
            default:
                ThrowInvalidType(type);
                break;
        }
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

    private void VisitNode(NodeBase node)
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
        node.IterateContent(this, (child, self) =>
        {
            self.Visit(child);
        });
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
