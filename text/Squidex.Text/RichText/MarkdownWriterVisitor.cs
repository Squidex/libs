using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Syntax;
using Squidex.Text.RichText.Model;

namespace Squidex.Text.RichText;

internal class MarkdownWriterVisitor : Visitor<Block?>
{
    private readonly QuoteBlockParser quoteBlockParser = new QuoteBlockParser();
    private readonly ListBlockParser listBlockParser = new ListBlockParser();
    private static readonly HeadingBlockParser HeadingParser = new HeadingBlockParser();

    protected override Block? VisitBlockquote(Node node)
    {
        var markdownNode = new QuoteBlock(quoteBlockParser);

        return AddChildren(node, markdownNode);
    }

    protected override Block? VisitBulletList(Node node)
    {
        var markdownNode = new ListBlock(listBlockParser) { IsOrdered = false };

        return AddChildren(node, markdownNode);
    }

    protected override Block? VisitCodeBlock(Node node)
    {
        return null!;
    }

    protected override Block? VisitDocument(Node node)
    {
        var markdownNode = new MarkdownDocument();

        return AddChildren(node, markdownNode);
    }

    protected override Block VisitHeading(Node node)
    {
        var headingBlock = new HeadingBlock(HeadingParser)
        {
            HeaderChar = '#',
            HeaderCharCount = (int)node.GetNumber("level", 1)
        };

        return AddText(node, headingBlock);
    }

    private static Block AddText(Node node, LeafBlock markdownNode)
    {
        if (node.Text != null)
        {
            markdownNode.Lines.Add(new StringSlice(node.Text));
        }

        if (node.Content != null)
        {
            foreach (var child in node.Content)
            {
                AddText(child, markdownNode);
            }
        }

        return markdownNode;
    }

    private Block AddChildren(Node node, ContainerBlock markdownNode)
    {
        if (node.Content != null)
        {
            foreach (var child in node.Content)
            {
                var childResult = Visit(child);

                if (childResult != null)
                {
                    markdownNode.Add(childResult);
                }
            }
        }

        return markdownNode;
    }
}
