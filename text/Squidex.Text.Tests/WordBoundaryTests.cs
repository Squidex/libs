// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text;

public class WordBoundaryTests
{
    [Theory]
    [InlineData("abc", 1)]
    [InlineData("åäö", 1)]
    [InlineData("üßœ", 1)]
    public void Should_not_break_between_most_characters(string text, int index)
    {
        Assert.False(IsWordBoundary(text, index));
    }

    [Theory]
    [InlineData("can't", 2)]
    [InlineData("can't", 3)]
    [InlineData("can’t", 2)]
    [InlineData("can’t", 3)]
    [InlineData("foo.bar", 2)]
    [InlineData("foo.bar", 3)]
    [InlineData("foo:bar", 2)]
    [InlineData("foo:bar", 3)]
    public void Should_not_break_some_punctuation(string text, int index)
    {
        Assert.False(IsWordBoundary(text, index));
    }

    [Theory]
    [InlineData("123", 1)]
    [InlineData("a123", 1)]
    [InlineData("1a23", 1)]
    public void Should_not_break_on_characters_attached_to_numbers(string text, int index)
    {
        Assert.False(IsWordBoundary(text, index));
    }

    [Theory]
    [InlineData("3.14", 1)]
    [InlineData("3.14", 2)]
    [InlineData("1,024", 1)]
    [InlineData("1,024", 2)]
    [InlineData("5-1", 1)]
    public void Should_break_on_punctuation_in_number_sequences(string text, int index)
    {
        Assert.False(IsWordBoundary(text, index));
    }

    [Theory]
    [InlineData("カラテ", 1)]
    public void Should_not_break_in_katakana(string text, int index)
    {
        Assert.False(IsWordBoundary(text, index));
    }

    [Theory]
    [InlineData("空手道", 1)]
    public void Should_break_between_every_kanji(string text, int index)
    {
        Assert.True(IsWordBoundary(text, index));
    }

    [Theory]
    [InlineData("foo\r\nbar", 3)]
    public void Should_not_break_inside_clrf(string text, int index)
    {
        Assert.False(IsWordBoundary(text, index));
    }

    [Theory]
    [InlineData("foo\r\nbarr", 4)]
    [InlineData("foo\rbar", 3)]
    [InlineData("foo\nbar", 3)]
    public void Should_break_after_newlines(string text, int index)
    {
        Assert.True(IsWordBoundary(text, index));
    }

    [Theory]
    [InlineData("foo bar", 2)]
    [InlineData("foo\tbar", 2)]
    [InlineData("foo&bar", 2)]
    [InlineData("foo\"bar", 2)]
    [InlineData("foo(bar)", 2)]
    [InlineData("foo/bar", 2)]
    public void Should_break_everywhere_else(string text, int index)
    {
        Assert.True(IsWordBoundary(text, index));
    }

    [Theory]
    [InlineData("foo", 5)]
    [InlineData("foo", -1)]
    public void Should_not_break_when_given_out_of_bounds_index(string text, int index)
    {
        Assert.False(IsWordBoundary(text, index));
    }

    [Fact]
    public void Should_break_for_empty_string()
    {
        Assert.True(IsWordBoundary(string.Empty, 0));
    }

    private static bool IsWordBoundary(string text, int index)
    {
        return WordBoundary.IsBoundary(text, index);
    }
}
