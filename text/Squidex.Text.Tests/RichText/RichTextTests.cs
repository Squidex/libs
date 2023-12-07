// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Text.RichText.Model;
using Xunit;

namespace Squidex.Text.RichText;

public class RichTextTests
{
    [Fact]
    public void Should_render_paragraph()
    {
        var source = new Node
        {
            Type = NodeType.Paragraph,
            Content =
            [
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Paragraph1"
                },
            ],
        };

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
Paragraph1";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<p>
    Paragraph1
</p>";

        Assert.Equal(expectedHtml.TrimExpected(), html);
    }

    [Fact]
    public void Should_render_paragraphs()
    {
        var source = new Node
        {
            Type = NodeType.Document,
            Content =
            [
                new Node
                {
                    Type = NodeType.Paragraph,
                    Content =
                    [
                        new Node
                        {
                            Type = NodeType.Text,
                            Text = "Paragraph1"
                        },
                    ],
                },
                new Node
                {
                    Type = NodeType.Paragraph,
                    Content =
                    [
                        new Node
                        {
                            Type = NodeType.Text,
                            Text = "Paragraph2"
                        },
                    ],
                },
            ]
        };

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
Paragraph1

Paragraph2";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<p>
    Paragraph1
</p>
<p>
    Paragraph2
</p>";

        Assert.Equal(expectedHtml.TrimExpected(), html);
    }

    [Fact]
    public void Should_render_line_break()
    {
        var source = new Node
        {
            Type = NodeType.Paragraph,
            Content =
            [
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Paragraph1"
                },
                new Node
                {
                    Type = NodeType.HardBreak
                },
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Paragraph2"
                },
            ]
        };

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
Paragraph1
Paragraph2";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<p>
    Paragraph1
    <br />
    Paragraph2
</p>";

        Assert.Equal(expectedHtml.TrimExpected(), html);
    }

    [Fact]
    public void Should_render_horizontal_line()
    {
        var source = new Node
        {
            Type = NodeType.HorizontalLine
        };

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
---";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<hr>";

        Assert.Equal(expectedHtml.TrimExpected(), html);
    }

    [Fact]
    public void Should_render_code_block()
    {
        var source = new Node
        {
            Type = NodeType.CodeBlock,
            Attributes = new Attributes
            {
                ["language"] = new Model.Attribute(AttributeKind.String, "html")
            },
            Content =
            [
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Paragraph1"
                },
                new Node
                {
                    Type = NodeType.HardBreak
                },
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Paragraph2"
                },
            ]
        };

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
```html
Paragraph1
Paragraph2
```";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<pre spellcheck=""false"" class=""language-html"">
    <code data-code-block-lang=""html"">
        Paragraph1
        <br />
        Paragraph2
    </code>
</pre>";

        Assert.Equal(expectedHtml.TrimExpected(), html);
    }

    [Fact]
    public void Should_render_code_block_without_language()
    {
        var source = new Node
        {
            Type = NodeType.CodeBlock,
            Content =
            [
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Paragraph1"
                },
                new Node
                {
                    Type = NodeType.HardBreak
                },
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Paragraph2"
                },
            ]
        };

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
```
Paragraph1
Paragraph2
```";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<pre spellcheck=""false"">
    <code>
        Paragraph1
        <br />
        Paragraph2
    </code>
</pre>";

        Assert.Equal(expectedHtml.TrimExpected(), html);
    }
}
