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
            Type = NodeType.HorizontalRule
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
                                    Text = "Text1"
                                },
                            ]
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
                            Text = "Text2"
                        },
                    ]
                },
            ]
        };

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
> Text1

Text2";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<blockquote>
    <p>
        Text1
    </p>
</blockquote>
<p>
    Text2
</p>";

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
                ["language"] = "html"
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
                    Attributes = new Attributes()
                    {
                        ["href"] = "https://squidex.io",
                        ["target"] = "_blank"
                    }
                },
            ],
            Text = "Link Text"
        };

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
[Link Text](https://squidex.io)";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<a href=""https://squidex.io"" target=""_blank"">Link Text</a>";

        Assert.Equal(expectedHtml.TrimExpected(), html);
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
                    Type = MarkType.Link
                },
            ],
            Text = "Link Text"
        };

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
Link Text";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<a>Link Text</a>";

        Assert.Equal(expectedHtml.TrimExpected(), html);
    }

    [Fact]
    public void Should_render_image()
    {
        var source = new Node
        {
            Type = NodeType.Image,
            Attributes = new Attributes()
            {
                ["src"] = "https://squidex.io/logo.png",
                ["alt"] = "Logo",
                ["title"] = "Website Logo"
            }
        };

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
![Logo](https://squidex.io/logo.png ""Website Logo"")";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<img src=""https://squidex.io/logo.png"" alt=""Logo"" title=""Website Logo"" />";

        Assert.Equal(expectedHtml.TrimExpected(), html);
    }

    [Fact]
    public void Should_render_blank_image()
    {
        var source = new Node
        {
            Type = NodeType.Image,
            Attributes = new Attributes()
            {
                ["src"] = "https://squidex.io/logo.png"
            }
        };

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
![](https://squidex.io/logo.png)";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<img src=""https://squidex.io/logo.png"" />";

        Assert.Equal(expectedHtml.TrimExpected(), html);
    }
}
