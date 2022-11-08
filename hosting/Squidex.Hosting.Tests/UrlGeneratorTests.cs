// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using Squidex.Hosting.Web;
using Xunit;

namespace Squidex.Hosting;

public class UrlGeneratorTests
{
    [Theory]
    [InlineData("http://squidex.io")]
    [InlineData("http://squidex.io/")]
    public void Should_build_url_from_options(string url)
    {
        var sut = new UrlGenerator(Options.Create(new UrlOptions
        {
            BaseUrl = url,
            BasePath = null
        }));

        Assert.Equal("http://squidex.io", sut.BuildUrl());

        Assert.Equal("http://squidex.io", sut.BuildUrl("/", false));
        Assert.Equal("http://squidex.io", sut.BuildCallbackUrl("/", false));

        Assert.Equal("http://squidex.io/", sut.BuildUrl("/", true));
        Assert.Equal("http://squidex.io/", sut.BuildCallbackUrl("/", true));

        Assert.Equal("http://squidex.io/path", sut.BuildUrl("/path", false));
        Assert.Equal("http://squidex.io/path", sut.BuildUrl("/path/", false));

        Assert.Equal("http://squidex.io/path/", sut.BuildUrl("/path", true));
        Assert.Equal("http://squidex.io/path/", sut.BuildUrl("/path/", true));
    }

    [Theory]
    [InlineData("http://squidex.io")]
    [InlineData("http://squidex.io/")]
    [InlineData("http://squidex.io/base")]
    [InlineData("http://squidex.io/base/")]
    public void Should_build_url_from_options_with_base_path(string url)
    {
        var sut = new UrlGenerator(Options.Create(new UrlOptions
        {
            BaseUrl = url,
            BasePath = "base"
        }));

        Assert.Equal("http://squidex.io/base", sut.BuildUrl());

        Assert.Equal("http://squidex.io/base", sut.BuildUrl("/", false));
        Assert.Equal("http://squidex.io/base", sut.BuildCallbackUrl("/", false));

        Assert.Equal("http://squidex.io/base/", sut.BuildUrl("/", true));
        Assert.Equal("http://squidex.io/base/", sut.BuildCallbackUrl("/", true));

        Assert.Equal("http://squidex.io/base/path", sut.BuildUrl("/path", false));
        Assert.Equal("http://squidex.io/base/path", sut.BuildUrl("/path/", false));

        Assert.Equal("http://squidex.io/base/path/", sut.BuildUrl("/path", true));
        Assert.Equal("http://squidex.io/base/path/", sut.BuildUrl("/path/", true));
    }

    [Fact]
    public void Should_build_callback_url_if_configured()
    {
        var sut = new UrlGenerator(Options.Create(new UrlOptions
        {
            BaseUrl = "http://squidex.io",
            BasePath = null,
            CallbackUrl = "http://callback.squidex.io"
        }));

        Assert.Equal("http://callback.squidex.io", sut.BuildCallbackUrl());
        Assert.Equal("http://callback.squidex.io/", sut.BuildCallbackUrl("/"));
    }

    [Fact]
    public void Should_build_callback_url_if_not_configured()
    {
        var sut = new UrlGenerator(Options.Create(new UrlOptions
        {
            BaseUrl = "http://squidex.io",
            BasePath = null,
            CallbackUrl = null
        }));

        Assert.Equal("http://squidex.io", sut.BuildCallbackUrl());
        Assert.Equal("http://squidex.io/", sut.BuildCallbackUrl("/"));
    }
}
