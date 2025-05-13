// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text;

public class SlugifyTests
{
    [Theory]
    [InlineData("Hello World", '-', "hello-world")]
    [InlineData("Hello/World", '-', "hello-world")]
    [InlineData("Hello World", '_', "hello_world")]
    [InlineData("Hello/World", '_', "hello_world")]
    [InlineData("Hello World ", '_', "hello_world")]
    [InlineData("Hello World-", '_', "hello_world")]
    [InlineData("Hello/World_", '_', "hello_world")]
    public void Should_replace_special_characters_with_sepator_when_slugifying(string input, char separator, string output)
    {
        Assert.Equal(output, input.Slugify(separator: separator));
    }

    [Theory]
    [InlineData("ö", "oe")]
    [InlineData("ü", "ue")]
    [InlineData("ä", "ae")]
    public void Should_replace_multi_char_diacritics_when_slugifying(string input, string output)
    {
        Assert.Equal(output, input.Slugify());
    }

    [Theory]
    [InlineData("ö", "o")]
    [InlineData("ü", "u")]
    [InlineData("ä", "a")]
    public void Should_not_replace_multi_char_diacritics_when_slugifying(string input, string output)
    {
        Assert.Equal(output, input.Slugify(singleCharDiactric: true));
    }

    [Theory]
    [InlineData("Físh", "fish")]
    [InlineData("źish", "zish")]
    [InlineData("żish", "zish")]
    [InlineData("fórm", "form")]
    [InlineData("fòrm", "form")]
    [InlineData("fårt", "fart")]
    public void Should_replace_single_char_diacritics_when_slugifying(string input, string output)
    {
        Assert.Equal(output, input.Slugify());
    }

    [Theory]
    [InlineData("Hello my&World ", '_', "hello_my&world")]
    [InlineData("Hello my&World-", '_', "hello_my&world")]
    [InlineData("Hello my/World_", '_', "hello_my/world")]
    public void Should_keep_characters_when_slugifying(string input, char separator, string output)
    {
        Assert.Equal(output, input.Slugify(new HashSet<char> { '&', '/' }, false, separator));
    }
}
