// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;

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

    [Fact]
    public void Should_keep_whitespaces_between_inline_elements()
    {
        var html = "<p>Hello <strong>big</strong> World</p>";

        var text = html.Html2Text();

        Assert.Equal("Hello big World", text);
    }

    [Fact]
    public void Should_collapse_whitespaces()
    {
        var html = "<p>  Hello \n\n   World  </p>";

        var text = html.Html2Text();

        Assert.Equal("Hello World", text);
    }

    [Fact]
    public void Should_decode_entities_only_once()
    {
        var html = "<p>Hello &amp; World &amp;lt;</p>";

        var text = html.Html2Text();

        Assert.Equal("Hello & World &lt;", text);
    }

    [Fact]
    public void Should_convert_html_with_block_elements_to_text()
    {
        var html = "<div>Hello<br>World</div><ul><li>Item1</li><li>Item2</li></ul><h1>Title</h1>";

        var text = html.Html2Text();

        Assert.Equal(BuildText("Hello\nWorld\nItem1\nItem2\nTitle"), text);
    }

    [Fact]
    public void Should_not_convert_script_with_nested_tags_to_text()
    {
        var html = "<p>Hello</p><script>var x = '<b>bold</b>';</script><p>World</p>";

        var text = html.Html2Text();

        Assert.Equal(BuildText("Hello\nWorld"), text);
    }

    [Theory]
    [InlineData("1 &lt; 2", "1 < 2")]
    [InlineData("1 &xt; 2", "1 &xt; 2")]
    [InlineData("1 &#60; 2", "1 < 2")]
    [InlineData("1 &#x3C; 2", "1 < 2")]
    [InlineData("1 &; 2", "1 &; 2")]
    [InlineData("A & B", "A & B")]
    [InlineData("A & B &lt; C", "A & B < C")]
    [InlineData("A &", "A &")]
    [InlineData("A &amp", "A &amp")]
    [InlineData("A &&lt; B", "A &< B")]
    [InlineData("A &thetasym; B", "A ϑ B")]
    [InlineData("A &alefsym; B", "A ℵ B")]
    [InlineData("A &thisisaverylongnamethatisnotanentityatallandshouldbekept; B", "A &thisisaverylongnamethatisnotanentityatallandshouldbekept; B")]
    public void Should_convert_entity(string source, string expected)
    {
        var sb = new StringBuilder();

        HtmlEntity.Decode(source.AsMemory(), sb);

        Assert.Equal(expected, sb.ToString());
    }

    [Fact]
    public void Should_extract_metadata()
    {
        var svg = File.ReadAllText(Path.Combine("TestFiles", "SvgValid.svg"));

        var metadata = svg.GetSvgMetadata();

        Assert.Equal(new SvgMetadata("50", "30", "0 0 100 100"), metadata);
    }

    private static string BuildText(string text)
    {
        return text.Replace("\n", Environment.NewLine, StringComparison.Ordinal);
    }
}
