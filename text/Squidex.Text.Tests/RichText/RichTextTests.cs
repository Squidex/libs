// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Text.RichText.Model;

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
                    Text = "Paragraph1",
                },
            ],
        };

        RenderUtils.AssertNode(source,
            markdown: @"
Paragraph1",
            html: @"
<p>Paragraph1</p>",
            minHtml: @"
<p>Paragraph1</p>",
            text: @"
Paragraph1");
    }

    [Fact]
    public void Should_render_paragraphs()
    {
        var source = new Node
        {
            Type = NodeType.Doc,
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
                            Text = "Paragraph1",
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
                            Text = "Paragraph2",
                        },
                    ],
                },
            ],
        };

        RenderUtils.AssertNode(source,
            markdown: @"
Paragraph1

Paragraph2",
            html: @"
<p>Paragraph1</p>
<p>Paragraph2</p>",
            minHtml: @"
<p>Paragraph1</p><p>Paragraph2</p>",
            text: @"
Paragraph1 Paragraph2");
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
                    Text = "Paragraph1",
                },
                new Node
                {
                    Type = NodeType.HardBreak,
                },
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Paragraph2",
                },
            ],
        };

        RenderUtils.AssertNode(source,
            markdown: @"
Paragraph1
Paragraph2",
            html: @"
<p>
    Paragraph1
    <br>
    Paragraph2
</p>",
            minHtml: @"
<p>Paragraph1<br>Paragraph2</p>");
    }

    [Fact]
    public void Should_render_horizontal_line()
    {
        var source = new Node
        {
            Type = NodeType.HorizontalRule,
        };

        RenderUtils.AssertNode(source,
            markdown: @"
---",
            html: @"
<hr>",
            minHtml: @"
<hr>");
    }

    [Fact]
    public void Should_render_block_quote()
    {
        var source = new Node
        {
            Type = NodeType.Doc,
            Content =
            [
                new Node
                {
                    Type = NodeType.Blockquote,
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
                                    Text = "Text1",
                                },
                            ],
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
                            Text = "Text2",
                        },
                    ],
                },
            ],
        };

        RenderUtils.AssertNode(source,
            markdown: @"
> Text1

Text2",
            html: @"
<blockquote>
    <p>Text1</p>
</blockquote>
<p>Text2</p>",
            minHtml: @"
<blockquote><p>Text1</p></blockquote><p>Text2</p>");
    }

    [Fact]
    public void Should_render_code_block()
    {
        var source = new Node
        {
            Type = NodeType.CodeBlock,
            Attributes = new Attributes
            {
                ["language"] = "html",
            },
            Content =
            [
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Paragraph1",
                },
                new Node
                {
                    Type = NodeType.HardBreak,
                },
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Paragraph2",
                },
            ],
        };

        RenderUtils.AssertNode(source,
            markdown: @"
```html
Paragraph1
Paragraph2
```",
            html: @"
<pre class=""language-html"">
    <code data-code-block-language=""html"">
        Paragraph1
        <br>
        Paragraph2
    </code>
</pre>",
            minHtml: @"
<pre class=""language-html""><code data-code-block-language=""html"">Paragraph1<br>Paragraph2</code></pre>");
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
                    Text = "Paragraph1",
                },
                new Node
                {
                    Type = NodeType.HardBreak,
                },
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Paragraph2",
                },
            ],
        };

        RenderUtils.AssertNode(source,
            markdown: @"
```
Paragraph1
Paragraph2
```",
            html: @"
<pre>
    <code>
        Paragraph1
        <br>
        Paragraph2
    </code>
</pre>",
            minHtml: @"
<pre><code>Paragraph1<br>Paragraph2</code></pre>");
    }

    [Fact]
    public void Should_render_link()
    {
        var source = new Node
        {
            Type = NodeType.Text,
            Marks =
            [
                new Mark
                {
                    Type = MarkType.Link,
                    Attributes = new Attributes
                    {
                        ["href"] = "https://squidex.io",
                        ["target"] = "_blank",
                    },
                },
            ],
            Text = "Link Text",
        };

        RenderUtils.AssertNode(source,
            markdown: @"
[Link Text](https://squidex.io)",
            html: @"
<a href=""https://squidex.io"" target=""_blank"" rel=""noopener noreferrer nofollow"">Link Text</a>",
            minHtml: @"
<a href=""https://squidex.io"" target=""_blank"" rel=""noopener noreferrer nofollow"">Link Text</a>");
    }

    [Fact]
    public void Should_render_empty_link()
    {
        var source = new Node
        {
            Type = NodeType.Text,
            Marks =
            [
                new Mark
                {
                    Type = MarkType.Link,
                },
            ],
            Text = "Link Text",
        };

        RenderUtils.AssertNode(source,
            markdown: @"
Link Text",
            html: @"
<a rel=""noopener noreferrer nofollow"">Link Text</a>",
            minHtml: @"
<a rel=""noopener noreferrer nofollow"">Link Text</a>");
    }

    [Fact]
    public void Should_render_image()
    {
        var source = new Node
        {
            Type = NodeType.Image,
            Attributes = new Attributes
            {
                ["src"] = "https://squidex.io/logo.png",
                ["alt"] = "Logo",
                ["title"] = "Website Logo",
            },
        };

        RenderUtils.AssertNode(source,
            markdown: @"
![Logo](https://squidex.io/logo.png ""Website Logo"")",
            html: @"
<img alt=""Logo"" src=""https://squidex.io/logo.png"" title=""Website Logo"">",
            minHtml: @"
<img alt=""Logo"" src=""https://squidex.io/logo.png"" title=""Website Logo"">");
    }

    [Fact]
    public void Should_render_blank_image()
    {
        var source = new Node
        {
            Type = NodeType.Image,
            Attributes = new Attributes
            {
                ["src"] = "https://squidex.io/logo.png",
            },
        };

        RenderUtils.AssertNode(source,
            markdown: @"
![](https://squidex.io/logo.png)",
            html: @"
<img src=""https://squidex.io/logo.png"">",
            minHtml: @"
<img src=""https://squidex.io/logo.png"">");
    }

    [Fact]
    public void Should_render_heading()
    {
        var source = new Node
        {
            Type = NodeType.Heading,
            Content = [
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Heading 1",
                },
            ],
        };

        RenderUtils.AssertNode(source,
            markdown: @"
# Heading 1",
            html: @"
<h1>Heading 1</h1>",
            minHtml: @"
<h1>Heading 1</h1>");
    }

    [Fact]
    public void Should_render_heading1()
    {
        var source = new Node
        {
            Type = NodeType.Heading,
            Attributes = new Attributes
            {
                ["level"] = 1,
            },
            Content = [
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Heading 1",
                },
            ],
        };

        RenderUtils.AssertNode(source,
            markdown: @"
# Heading 1",
            html: @"
<h1>Heading 1</h1>",
            minHtml: @"
<h1>Heading 1</h1>");
    }

    [Fact]
    public void Should_render_heading2()
    {
        var source = new Node
        {
            Type = NodeType.Heading,
            Attributes = new Attributes
            {
                ["level"] = 2,
            },
            Content = [
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Heading 2",
                },
            ],
        };

        RenderUtils.AssertNode(source,
            markdown: @"
## Heading 2",
            html: @"
<h2>Heading 2</h2>",
            minHtml: @"
<h2>Heading 2</h2>");
    }

    [Fact]
    public void Should_render_max_length()
    {
        var source = new Node
        {
            Type = NodeType.Text,
            Text = "Paragraph1",
        };

        var actual = RenderUtils.RenderText(source, 4);

        Assert.Equal("Para", actual);
    }
}
