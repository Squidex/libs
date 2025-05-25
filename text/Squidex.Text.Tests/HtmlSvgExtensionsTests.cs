// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text;

public class HtmlSvgExtensionsTests
{
    [Theory]
    [InlineData("SvgValid.svg")]
    public void Should_return_true_for_valid_svg(string fileName)
    {
        var svg = File.ReadAllText(Path.Combine("TestFiles", fileName));

        var isValid = svg.IsValidSvg();

        Assert.True(isValid);
    }

    [Theory]
    [InlineData("SvgValid.svg")]
    public void Should_return_empty_errors_for_valid_svg(string fileName)
    {
        var svg = File.ReadAllText(Path.Combine("TestFiles", fileName));

        var errors = svg.GetSvgErrors();

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("SvgBase64Use.svg")]
    [InlineData("SvgIframeTag.svg")]
    [InlineData("SvgScriptAttribute.svg")]
    [InlineData("SvgScriptTag.svg")]
    public void Should_return_false_for_invalid_svg(string fileName)
    {
        var svg = File.ReadAllText(Path.Combine("TestFiles", fileName));

        var isValid = svg.IsValidSvg();

        Assert.False(isValid);
    }

    [Theory]
    [InlineData("SvgBase64Use.svg")]
    [InlineData("SvgIframeTag.svg")]
    [InlineData("SvgScriptAttribute.svg")]
    [InlineData("SvgScriptTag.svg")]
    public void Should_return_nonempty_errors_for_invalid_svg(string fileName)
    {
        var svg = File.ReadAllText(Path.Combine("TestFiles", fileName));

        var errors = svg.GetSvgErrors();

        Assert.NotEmpty(errors);
    }
}
