// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable RECS0015 // If an extension method is called as static method convert it to method syntax

namespace Squidex.Text;

public class WordsTests
{
    [Fact]
    public void Should_count_zero_words_for_empty_text()
    {
        Assert.Equal(0, WordExtensions.WordCount(string.Empty));
    }

    [Fact]
    public void Should_count_zero_words_for_whitspace_text()
    {
        Assert.Equal(0, WordExtensions.WordCount("  "));
    }

    [Fact]
    public void Should_count_english_words()
    {
        Assert.Equal(4, WordExtensions.WordCount("You can't do that"));
    }

    [Fact]
    public void Should_count_with_digits()
    {
        Assert.Equal(2, WordExtensions.WordCount("Hello 123"));
    }

    [Fact]
    public void Should_count_english_words_with_punctuation()
    {
        Assert.Equal(5, WordExtensions.WordCount("You can't do that, Mister."));
    }

    [Fact]
    public void Should_count_english_words_with_extra_whitespaces()
    {
        Assert.Equal(4, WordExtensions.WordCount("You can't do  that. "));
    }

    [Fact]
    public void Should_count_cjk_and_english_words()
    {
        Assert.Equal(7, WordExtensions.WordCount("Makes probably no sense: 空手道"));
    }

    [Fact]
    public void Should_count_cjk_words()
    {
        Assert.Equal(3, WordExtensions.WordCount("空手道"));
    }

    [Fact]
    public void Should_count_katakana_words()
    {
        Assert.Equal(3, WordExtensions.WordCount("カラテ カラテ カラテ"));
    }
}
