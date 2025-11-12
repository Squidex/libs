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
    private readonly RichTextOptions options;
    private INode currentNode;

    public bool IsLastInContainer { get; private set; }

    public bool IsFirstInContainer { get; private set; }

    protected Visitor(RichTextOptions options)
    {
        this.options = options;
        visitInner = VisitCurrentMarkOrNode;
    }

    public void VisitRoot(INode node)
    {
        IsLastInContainer = true;
        IsFirstInContainer = true;

        Visit(node);
    }

    protected virtual void Visit(INode node)
    {
        currentNode = node;
        currentNode.Reset();

        VisitCurrentMarkOrNode();
    }

    private void VisitCurrentMarkOrNode()
    {
        var mark = currentNode.GetNextMark(options);

        if (mark != null)
        {
            VisitMark(mark);
        }
        else
        {
            VisitNode(currentNode);
        }
    }

    private void VisitMark(IMark mark)
    {
        var type = mark.Type;

        switch (type)
        {
            case MarkType.Link:
                VisitLink(mark, visitInner,
                    mark.GetStringAttr("href"),
                    mark.GetStringAttr("target"),
                    "noopener noreferrer nofollow");
                break;
            case MarkType.ClassName:
                VisitClassName(mark, visitInner,
                    mark.GetStringAttr("className"));
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
                visitInner();
                break;
        }
    }

    protected virtual void VisitBold(IMark mark, Action inner)
    {
        inner();
    }

    protected virtual void VisitClassName(IMark mark, Action inner, string className)
    {
        inner();
    }

    protected virtual void VisitCode(IMark mark, Action inner)
    {
        inner();
    }

    protected virtual void VisitItalic(IMark mark, Action inner)
    {
        inner();
    }

    protected virtual void VisitLink(IMark mark, Action inner, string? href, string? target, string rel)
    {
        inner();
    }

    protected virtual void VisitUnderline(IMark mark, Action inner)
    {
        inner();
    }

    private void VisitNode(INode node)
    {
        var type = node.Type;

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
            case NodeType.Doc:
                VisitDocument(node);
                break;
            case NodeType.HardBreak:
                VisitHardBreak(node);
                break;
            case NodeType.HorizontalRule:
                VisitHorizontalRule(node);
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
                VisitChildren(node);
                break;
        }
    }

    protected virtual void VisitBlockquote(INode node)
    {
        VisitChildren(node);
    }

    protected virtual void VisitBulletList(INode node)
    {
        VisitChildren(node);
    }

    protected virtual void VisitCodeBlock(INode node, string? language)
    {
        VisitChildren(node);
    }

    protected virtual void VisitDocument(INode node)
    {
        VisitChildren(node);
    }

    protected virtual void VisitHardBreak(INode node)
    {
        VisitChildren(node);
    }

    protected virtual void VisitHeading(INode node, int level)
    {
        VisitChildren(node);
    }

    protected virtual void VisitHorizontalRule(INode node)
    {
        VisitChildren(node);
    }

    protected virtual void VisitImage(INode node, string? src, string? alt, string? title)
    {
        VisitChildren(node);
    }

    protected virtual void VisitListItem(INode node)
    {
        VisitChildren(node);
    }

    protected virtual void VisitOrderedList(INode node)
    {
        VisitChildren(node);
    }

    protected virtual void VisitParagraph(INode node)
    {
        VisitChildren(node);
    }

    protected virtual void VisitText(INode node)
    {
        VisitChildren(node);
    }

    protected void IterateChildren<T>(INode node, T state, Action<INode, T> action)
    {
        var prevIsLastInContainer = IsLastInContainer;
        var prevIsFirstInContainer = IsFirstInContainer;

        node.IterateContent((self: this, state, action), options, static (child, s, isFirst, isLast) =>
        {
            s.self.IsFirstInContainer = isFirst;
            s.self.IsLastInContainer = isLast;

            s.action(child, s.state);
        });

        IsLastInContainer = prevIsLastInContainer;
        IsFirstInContainer = prevIsFirstInContainer;
    }

    protected virtual void VisitChildren(INode node)
    {
        IterateChildren(node, this, static (child, self) =>
        {
            self.Visit(child);
        });
    }
}
