// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Events;

public class StreamFilterTests
{
    [Fact]
    public void Should_simplify_input_to_default_filter()
    {
        var sut = new StreamFilter(StreamFilterKind.MatchFull);

        Assert.Equal(default, sut);
    }

    [Fact]
    public void Should_create_empty_full_filter()
    {
        var sut = StreamFilter.Name();

        Assert.Equal(StreamFilterKind.MatchFull, sut.Kind);
        Assert.Empty(sut.Prefixes!.ToArray());
    }

    [Fact]
    public void Should_create_full_filter()
    {
        var sut = StreamFilter.Name("a", "b", "c");

        Assert.Equal(StreamFilterKind.MatchFull, sut.Kind);
        Assert.Equal(new[] { "a", "b", "c" }, sut.Prefixes!.ToArray());
    }

    [Fact]
    public void Should_create_empty_prefix_filter()
    {
        var sut = StreamFilter.Name();

        Assert.Equal(StreamFilterKind.MatchFull, sut.Kind);
        Assert.Empty(sut.Prefixes!.ToArray());
    }

    [Fact]
    public void Should_create_prefix_filter()
    {
        var sut = StreamFilter.Prefix("a", "b", "c");

        Assert.Equal(StreamFilterKind.MatchStart, sut.Kind);
        Assert.Equal(new[] { "a", "b", "c" }, sut.Prefixes!.ToArray());
    }

    [Fact]
    public void Should_implement_equals()
    {
        var prefix1 = StreamFilter.Prefix("a", "b", "c");
        var prefix2 = StreamFilter.Prefix("a", "b", "c");
        var prefix3 = StreamFilter.Prefix("c", "a", "b");
        var prefix4 = StreamFilter.Prefix("a", "b", "c", "d");
        var prefix5 = StreamFilter.Prefix();
        var prefix6 = new StreamFilter(StreamFilterKind.MatchStart);

        var name1 = StreamFilter.Name("a", "b", "c");
        var name2 = StreamFilter.Name("a", "b", "c");
        var name3 = StreamFilter.Name("c", "a", "b");
        var name4 = StreamFilter.Name("a", "b", "c", "d");
        var name5 = StreamFilter.Name();
        var name6 = new StreamFilter(StreamFilterKind.MatchFull);

        static void AssertEqual(StreamFilter lhs, StreamFilter rhs)
        {
            Assert.Equal(lhs, rhs);
            Assert.Equal(lhs.GetHashCode(), rhs.GetHashCode());
            Assert.True(lhs == rhs);
        }

        static void AssertNotEqual(StreamFilter lhs, StreamFilter rhs)
        {
            Assert.NotEqual(lhs, rhs);
            Assert.NotEqual(lhs.GetHashCode(), rhs.GetHashCode());
            Assert.True(lhs != rhs);
        }

        AssertEqual(prefix1, prefix2);
        AssertEqual(prefix1, prefix3);
        AssertEqual(prefix5, prefix6);
        AssertNotEqual(prefix2, prefix4);

        AssertEqual(name1, name2);
        AssertEqual(name1, name3);
        AssertEqual(name5, name6);
        AssertNotEqual(name2, name4);

        AssertNotEqual(prefix1, name1);
    }
}
