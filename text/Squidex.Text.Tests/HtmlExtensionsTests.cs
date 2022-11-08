// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Text;

public class HtmlExtensionsTests
{
    [Fact]
    public void Should_convert_html_with_paragraph_to_text()
    {
        var html = "<p>Hello</p><p>World</p";

        var text = html.Html2Text();

        Assert.Equal(BuildText("Hello\nWorld"), text);
    }

    [Fact]
    public void Should_convert_html_with_break_to_text()
    {
        var html = "<div>Hello</br>World</div>";

        var text = html.Html2Text();

        Assert.Equal(BuildText("Hello\nWorld"), text);
    }

    [Fact]
    public void Should_not_convert_html_with_attribute_to_text()
    {
        var html = "<img alt=\"Hello World\" />";

        var text = html.Html2Text();

        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public void Should_not_convert_html_with_style_to_text()
    {
        var html = "<style>Hello World</style>";

        var text = html.Html2Text();

        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public void Should_not_convert_html_with_script_to_text()
    {
        var html = "<script>Hello World</script>";

        var text = html.Html2Text();

        Assert.Equal(string.Empty, text);
    }

    private static string BuildText(string text)
    {
        return text.Replace("\n", Environment.NewLine, StringComparison.Ordinal);
    }
}
