// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable RECS0015 // If an extension method is called as static method convert it to method syntax

namespace Squidex.Text;

public class CharactersTests
{
    [Fact]
    public void Should_count_zero_characters_for_empty_text()
    {
        Assert.Equal(0, CharactersExtensions.CharacterCount(string.Empty));
    }

    [Fact]
    public void Should_count_zero_characters_for_whitspace_text()
    {
        Assert.Equal(0, CharactersExtensions.CharacterCount("  "));
    }

    [Fact]
    public void Should_count_english_characters()
    {
        Assert.Equal(13, CharactersExtensions.CharacterCount("You can't do that"));
    }

    [Fact]
    public void Should_count_with_digits()
    {
        Assert.Equal(8, CharactersExtensions.CharacterCount("Hello 123"));
    }

    [Fact]
    public void Should_count_english_characters_with_punctuation()
    {
        Assert.Equal(19, CharactersExtensions.CharacterCount("You can't do that, Mister."));
    }

    [Fact]
    public void Should_count_english_characters_with_extra_whitespaces()
    {
        Assert.Equal(13, CharactersExtensions.CharacterCount("You can't do  that. "));
    }

    [Fact]
    public void Should_count_cjk_and_english_words()
    {
        Assert.Equal(23, CharactersExtensions.CharacterCount("Makes probably no sense: 空手道"));
    }

    [Fact]
    public void Should_count_cjk_words()
    {
        Assert.Equal(3, CharactersExtensions.CharacterCount("空手道"));
    }

    [Fact]
    public void Should_count_katakana_words()
    {
        Assert.Equal(9, CharactersExtensions.CharacterCount("カラテ カラテ カラテ"));
    }
}
